// StringMeasure.cpp 
// Wrapper around DirectWrite to export the way of measuring strings
// that Rainmeter uses. This fixes a problem that occurs since GDI+
// measures Unicode characters such as emojis differently causing
// the word wrap and image placement to be incorrect.
//
// Code is stolen (and very slightly modified) from the Rainmeter
// source code.

#include <ole2.h>
#include <GdiPlus.h>
#include <memory>
#include <string>
#include <dwrite_1.h>
#include <wrl/client.h>
#include <d2d1_1.h>
#include <d2d1helper.h>

Microsoft::WRL::ComPtr<IDWriteTextFormat> m_TextFormat;
float m_ExtraHeight;
float m_LineGap;
Microsoft::WRL::ComPtr<IDWriteFactory1> c_DWFactory;
Microsoft::WRL::ComPtr<IDWriteGdiInterop> c_DWGDIInterop;
bool gdiEmulation;

struct Size {
	float Width;
	float Height;
};


HRESULT GetDWritePropertiesFromGDIProperties(
	IDWriteFactory* factory, const WCHAR* gdiFamilyName, const bool gdiBold, const bool gdiItalic,
	DWRITE_FONT_WEIGHT& dwriteFontWeight, DWRITE_FONT_STYLE& dwriteFontStyle,
	DWRITE_FONT_STRETCH& dwriteFontStretch, WCHAR* dwriteFamilyName, UINT dwriteFamilyNameSize);
void GetPropertiesFromDWriteFont(
	IDWriteFont* dwriteFont, const bool bold, const bool italic,
	DWRITE_FONT_WEIGHT* dwriteFontWeight, DWRITE_FONT_STYLE* dwriteFontStyle,
	DWRITE_FONT_STRETCH* dwriteFontStretch);
IDWriteFont* CreateDWriteFontFromGDIFamilyName(IDWriteFactory* factory, const WCHAR* gdiFamilyName);
HRESULT GetFamilyNameFromDWriteFont(IDWriteFont* font, WCHAR* buffer, const UINT bufferSize);
HRESULT GetFamilyNameFromDWriteFontFamily(IDWriteFontFamily* fontFamily, WCHAR* buffer, const UINT bufferSize);
bool IsFamilyInSystemFontCollection(IDWriteFactory* factory, const WCHAR* familyName);
HRESULT GetGDIFamilyNameFromDWriteFont(IDWriteFont* font, WCHAR* buffer, UINT bufferSize);
IDWriteFont* FindDWriteFontInFontFamilyByGDIFamilyName(IDWriteFontFamily* fontFamily, const WCHAR* gdiFamilyName);
IDWriteFont* FindDWriteFontInFontCollectionByGDIFamilyName(IDWriteFontCollection* fontCollection, const WCHAR* gdiFamilyName);

EXTERN_C __declspec(dllexport) void InitializeMeasurer(const WCHAR* fontFamily, int size, bool bold, bool italic, bool accurateText)
{
	gdiEmulation = !accurateText;
	HRESULT hr = DWriteCreateFactory(
		DWRITE_FACTORY_TYPE_SHARED,
		__uuidof(c_DWFactory),
		(IUnknown**)c_DWFactory.GetAddressOf());
	if (FAILED(hr)) return;

	hr = c_DWFactory->GetGdiInterop(c_DWGDIInterop.GetAddressOf());
	if (FAILED(hr)) return;

	DWRITE_FONT_WEIGHT dwriteFontWeight =
		bold ? DWRITE_FONT_WEIGHT_BOLD : DWRITE_FONT_WEIGHT_REGULAR;
	DWRITE_FONT_STYLE dwriteFontStyle =
		italic ? DWRITE_FONT_STYLE_ITALIC : DWRITE_FONT_STYLE_NORMAL;
	DWRITE_FONT_STRETCH dwriteFontStretch = DWRITE_FONT_STRETCH_NORMAL;
	const float dwriteFontSize = size * (4.0f / 3.0f);
	WCHAR dwriteFamilyName[LF_FACESIZE];

	// obtained from it.
	hr = GetDWritePropertiesFromGDIProperties(
		c_DWFactory.Get(), fontFamily, bold, italic, dwriteFontWeight, dwriteFontStyle,
		dwriteFontStretch, dwriteFamilyName, _countof(dwriteFamilyName));
	if (SUCCEEDED(hr))
	{
		hr = c_DWFactory->CreateTextFormat(
			dwriteFamilyName,
			nullptr,
			dwriteFontWeight,
			dwriteFontStyle,
			dwriteFontStretch,
			dwriteFontSize,
			L"",
			&m_TextFormat);
	}

	if (SUCCEEDED(hr))
	{
		m_TextFormat->SetTextAlignment(DWRITE_TEXT_ALIGNMENT_LEADING);
		m_TextFormat->SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT_NEAR);
		DWRITE_TRIMMING trimming;
		trimming.granularity = DWRITE_TRIMMING_GRANULARITY_NONE;
		m_TextFormat->SetTrimming(&trimming, nullptr);
		// Get the family name to in case CreateTextFormat() fallbacked on some other family name.
		hr = m_TextFormat->GetFontFamilyName(dwriteFamilyName, _countof(dwriteFamilyName));
		if (FAILED(hr)) return;


		Microsoft::WRL::ComPtr<IDWriteFontCollection> collection;
		Microsoft::WRL::ComPtr<IDWriteFontFamily> fontFamily;
		UINT32 familyNameIndex;
		BOOL exists;

		if (FAILED(m_TextFormat->GetFontCollection(collection.GetAddressOf())) ||
			FAILED(collection->FindFamilyName(dwriteFamilyName, &familyNameIndex, &exists)) ||
			FAILED(collection->GetFontFamily(familyNameIndex, fontFamily.GetAddressOf())))
		{
			return;
		}

		Microsoft::WRL::ComPtr<IDWriteFont> font;
		hr = fontFamily->GetFirstMatchingFont(
			m_TextFormat->GetFontWeight(),
			m_TextFormat->GetFontStretch(),
			m_TextFormat->GetFontStyle(),
			font.GetAddressOf());

		if (FAILED(hr)) return;

		DWRITE_FONT_METRICS fmetrics;
		font->GetMetrics(&fmetrics);

		// GDI+ compatibility: GDI+ adds extra padding below the string when |m_AccurateText| is
		// |false|. The bottom padding seems to be based on the font metrics so we can calculate it
		// once and keep using it regardless of the actual string. In some cases, GDI+ also adds
		// the line gap to the overall height so we will store it as well.
		const float pixelsPerDesignUnit = dwriteFontSize / (float)fmetrics.designUnitsPerEm;
		m_ExtraHeight =
			(((float)fmetrics.designUnitsPerEm / 8.0f) - fmetrics.lineGap) * pixelsPerDesignUnit;
		m_LineGap = fmetrics.lineGap * pixelsPerDesignUnit;
	}
}


EXTERN_C __declspec(dllexport) void GetTextSize(WCHAR* str, UINT32 strLen, OUT Size *size)
{
	// GDI+ compatibility: If the last character is a newline, GDI+ measurements seem to ignore it.
	bool strippedLastNewLine = false;
	if (strLen > 2 && str[strLen - 1] == L'\n')
	{
		strippedLastNewLine = true;
		--strLen;

		if (str[strLen - 1] == L'\r')
		{
			--strLen;
		}
	}

	DWRITE_TEXT_METRICS metrics = { 0 };
	Microsoft::WRL::ComPtr<IDWriteTextLayout> textLayout;
	HRESULT hr = c_DWFactory->CreateTextLayout(
		str,
		strLen,
		m_TextFormat.Get(),
		10000,
		10000,
		textLayout.GetAddressOf());

	if (FAILED(hr)) return;

	const float xOffset = m_TextFormat->GetFontSize() / 6.0f;
	if (gdiEmulation)
	{
		Microsoft::WRL::ComPtr<IDWriteTextLayout1> textLayout1;
		textLayout.As(&textLayout1);

		const float emOffset = xOffset / 24.0f;
		const DWRITE_TEXT_RANGE range = { 0, strLen };
		textLayout1->SetCharacterSpacing(emOffset, emOffset, 0.0f, range);
	}

	textLayout->GetMetrics(&metrics);
	if (metrics.width > 0.0f)
	{
		if (gdiEmulation)
		{
			metrics.width += xOffset * 2;
			metrics.height += m_ExtraHeight;

			// GDI+ compatibility: If the string contains a newline (even if it is the
			// stripped last character), GDI+ adds the line gap to the overall height.
			if (strippedLastNewLine || wmemchr(str, L'\n', strLen) != nullptr)
			{
				metrics.height += m_LineGap;
			}
		}
		else
		{
			// GDI+ compatibility: With accurate metrics, the line gap needs to be subtracted
			// from the overall height if the string does not contain newlines.
			if (!strippedLastNewLine && wmemchr(str, L'\n', strLen) == nullptr)
			{
				metrics.height -= m_LineGap;
			}
		}
	}
	else
	{
		// GDI+ compatibility: Get rid of the height that DirectWrite assigns to zero-width
		// strings.
		metrics.height = 0.0f;
	}
	size->Height = metrics.height;
	size->Width = metrics.widthIncludingTrailingWhitespace;
	if (size->Height > 0.0f)
	{
		// GDI+ draws multi-line text even though the last line may be clipped slightly at the
		// bottom. This is a workaround to emulate that behaviour.
		size->Height += 1.0f;
	}
}

EXTERN_C __declspec(dllexport) void DisposeMeasurer()
{
	m_TextFormat.Reset();
	c_DWGDIInterop.Reset();
	c_DWFactory.Reset();
}


// Font handling stuff


HRESULT GetDWritePropertiesFromGDIProperties(
	IDWriteFactory* factory, const WCHAR* gdiFamilyName, const bool gdiBold, const bool gdiItalic,
	DWRITE_FONT_WEIGHT& dwriteFontWeight, DWRITE_FONT_STYLE& dwriteFontStyle,
	DWRITE_FONT_STRETCH& dwriteFontStretch, WCHAR* dwriteFamilyName, UINT dwriteFamilyNameSize)
{
	HRESULT hr = E_FAIL;
	IDWriteFont* dwriteFont = CreateDWriteFontFromGDIFamilyName(factory, gdiFamilyName);
	if (dwriteFont)
	{
		hr = GetFamilyNameFromDWriteFont(dwriteFont, dwriteFamilyName, dwriteFamilyNameSize);
		if (SUCCEEDED(hr))
		{
			GetPropertiesFromDWriteFont(
				dwriteFont, gdiBold, gdiItalic, &dwriteFontWeight, &dwriteFontStyle, &dwriteFontStretch);
		}

		dwriteFont->Release();
	}

	return hr;
}

void GetPropertiesFromDWriteFont(
	IDWriteFont* dwriteFont, const bool bold, const bool italic,
	DWRITE_FONT_WEIGHT* dwriteFontWeight, DWRITE_FONT_STYLE* dwriteFontStyle,
	DWRITE_FONT_STRETCH* dwriteFontStretch)
{
	*dwriteFontWeight = dwriteFont->GetWeight();
	if (bold)
	{
		if (*dwriteFontWeight == DWRITE_FONT_WEIGHT_NORMAL)
		{
			*dwriteFontWeight = DWRITE_FONT_WEIGHT_BOLD;
		}
		else if (*dwriteFontWeight < DWRITE_FONT_WEIGHT_ULTRA_BOLD)
		{
			// If 'gdiFamilyName' was e.g. 'Segoe UI Light', |dwFontWeight| wil be equal to
			// DWRITE_FONT_WEIGHT_LIGHT. If |gdiBold| is true in that case, we need to
			// increase the weight a little more for similar results with GDI+.
			// TODO: Is +100 enough?
			*dwriteFontWeight = (DWRITE_FONT_WEIGHT)(*dwriteFontWeight + 100);
		}
	}

	*dwriteFontStyle = dwriteFont->GetStyle();
	if (italic && *dwriteFontStyle == DWRITE_FONT_STYLE_NORMAL)
	{
		*dwriteFontStyle = DWRITE_FONT_STYLE_ITALIC;
	}

	*dwriteFontStretch = dwriteFont->GetStretch();
}

IDWriteFont* CreateDWriteFontFromGDIFamilyName(IDWriteFactory* factory, const WCHAR* gdiFamilyName)
{
	Microsoft::WRL::ComPtr<IDWriteGdiInterop> dwGdiInterop;
	HRESULT hr = factory->GetGdiInterop(dwGdiInterop.GetAddressOf());
	if (SUCCEEDED(hr))
	{
		LOGFONT lf = {};
		wcscpy_s(lf.lfFaceName, gdiFamilyName);
		lf.lfHeight = -12;
		lf.lfWeight = FW_DONTCARE;
		lf.lfCharSet = DEFAULT_CHARSET;
		lf.lfOutPrecision = OUT_DEFAULT_PRECIS;
		lf.lfClipPrecision = CLIP_DEFAULT_PRECIS;
		lf.lfQuality = ANTIALIASED_QUALITY;
		lf.lfPitchAndFamily = VARIABLE_PITCH;

		IDWriteFont* dwFont;
		hr = dwGdiInterop->CreateFontFromLOGFONT(&lf, &dwFont);
		if (SUCCEEDED(hr))
		{
			return dwFont;
		}
	}

	return nullptr;
}

HRESULT GetFamilyNameFromDWriteFont(IDWriteFont* font, WCHAR* buffer, const UINT bufferSize)
{
	IDWriteFontFamily* dwriteFontFamily;
	HRESULT hr = font->GetFontFamily(&dwriteFontFamily);
	if (SUCCEEDED(hr))
	{
		hr = GetFamilyNameFromDWriteFontFamily(dwriteFontFamily, buffer, bufferSize);
		dwriteFontFamily->Release();
	}

	return hr;
}

HRESULT GetFamilyNameFromDWriteFontFamily(
	IDWriteFontFamily* fontFamily, WCHAR* buffer, const UINT bufferSize)
{
	IDWriteLocalizedStrings* dwFamilyNames;
	HRESULT hr = fontFamily->GetFamilyNames(&dwFamilyNames);
	if (SUCCEEDED(hr))
	{
		// TODO: Determine the best index?
		hr = dwFamilyNames->GetString(0, buffer, bufferSize);
		dwFamilyNames->Release();
	}

	return hr;
}

bool IsFamilyInSystemFontCollection(IDWriteFactory* factory, const WCHAR* familyName)
{
	bool result = false;
	IDWriteFontCollection* systemFontCollection;
	HRESULT hr = factory->GetSystemFontCollection(&systemFontCollection);
	if (SUCCEEDED(hr))
	{
		UINT32 familyNameIndex;
		BOOL familyNameFound;
		HRESULT hr = systemFontCollection->FindFamilyName(
			familyName, &familyNameIndex, &familyNameFound);
		if (SUCCEEDED(hr) && familyNameFound)
		{
			result = true;
		}

		systemFontCollection->Release();
	}

	return result;
}

HRESULT GetGDIFamilyNameFromDWriteFont(IDWriteFont* font, WCHAR* buffer, UINT bufferSize)
{
	Microsoft::WRL::ComPtr<IDWriteLocalizedStrings> strings;
	BOOL stringsExist;
	font->GetInformationalStrings(
		DWRITE_INFORMATIONAL_STRING_WIN32_FAMILY_NAMES, strings.GetAddressOf(), &stringsExist);
	if (strings && stringsExist)
	{
		return strings->GetString(0, buffer, bufferSize);
	}

	return E_FAIL;
}

IDWriteFont* FindDWriteFontInFontFamilyByGDIFamilyName(
	IDWriteFontFamily* fontFamily, const WCHAR* gdiFamilyName)
{
	const UINT32 fontFamilyFontCount = fontFamily->GetFontCount();
	for (UINT32 j = 0; j < fontFamilyFontCount; ++j)
	{
		IDWriteFont* font;
		HRESULT hr = fontFamily->GetFont(j, &font);
		if (SUCCEEDED(hr))
		{
			WCHAR buffer[LF_FACESIZE];
			hr = GetGDIFamilyNameFromDWriteFont(font, buffer, _countof(buffer));
			if (SUCCEEDED(hr) && _wcsicmp(gdiFamilyName, buffer) == 0)
			{
				return font;
			}

			font->Release();
		}
	}

	return nullptr;
}

IDWriteFont* FindDWriteFontInFontCollectionByGDIFamilyName(
	IDWriteFontCollection* fontCollection, const WCHAR* gdiFamilyName)
{
	const UINT32 fontCollectionFamilyCount = fontCollection->GetFontFamilyCount();
	for (UINT32 i = 0; i < fontCollectionFamilyCount; ++i)
	{
		IDWriteFontFamily* fontFamily;
		HRESULT hr = fontCollection->GetFontFamily(i, &fontFamily);
		if (SUCCEEDED(hr))
		{
			IDWriteFont* font = FindDWriteFontInFontFamilyByGDIFamilyName(
				fontFamily, gdiFamilyName);
			fontFamily->Release();

			if (font)
			{
				return font;
			}
		}
	}

	return nullptr;
}