// CLR.h

#pragma once

using namespace System;

namespace CLR {

	public ref class StringMeasureWrapper
	{

	public:
		StringMeasureWrapper() { m_nativeClass = new NativeClass(); }
		~StringMeasureWrapper() { this->!NativeClassWrapper(); }
		!StringMeasureWrapper() { delete m_nativeClass; }
		void Method() {
			m_nativeClass->Method();
		}
	};
}
