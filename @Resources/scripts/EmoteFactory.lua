-- @author Malody Hoe / GitHub: madhoe / Twitter: maddhoexD
-- Structure of Script Measure:
---- IncFile=
---- Number=
---- SectionName=
---- OptionN=
---- ValueN=
---- where N is an ordered number from 0
-- Use %% to substitute it as the iteration number (which is specified by the Number option)
---- For example, if you specify 10, it will create 10 sections and replace the first section's %%
---- with 0, the second section's %% with 1, etc...
-- Wrap any formulas you want to parse in {} that otherwise RM would treat as a string
---- For example, [Measure{%%+1}] will have this script parse it for you

function Initialize()
	local numEmotes = SELF:GetNumberOption("NumEmotes")
	local file = io.open(SKIN:MakePathAbsolute(SELF:GetOption("IncFile")), "w")
	
	local t = {}
	
	local names = {'ID', 'X', 'Y'}
	for i = 0, numEmotes-1 do
		for nameCount = 1, #names do
			local n = names[nameCount]
			local name = "TwitchEmote" .. n .. "%%"
			table.insert(t, "[" .. doSub(name, i) .. "]")
			insertOptions("Twitch", t, i)
			table.insert(t, "Type=" ..doSub(name, i))
			insertOptions("Twitch" .. n, t, i)
			table.insert(t, "")
		end
		table.insert(t, "[" .. doSub("TwitchEmoteImage%%", i) .. "]")
		insertOptions("Image", t, i)
		table.insert(t, "\n;------------------------------------------------;\n")
	end
	
	
	file:write(table.concat(t, "\n"))
	file:close()
end

function insertOptions(op, t, i)
	local j = 0
			
	while true do
		local opt = SELF:GetOption(op .."Option" .. j)
		if opt == "" then
			break
		end
		table.insert(t, opt .. "=" .. doSub(SELF:GetOption( op .. "Value" .. j), i))
		j = j + 1
	end
end	

-- does all the substitution!
function doSub(value, i)
	return value:gsub("%%%%", i):gsub("{.-}", parseFormula)
end

-- sub to remove {the curly braces}, then add (parentheses), then parse it
function parseFormula(formula)
	return SKIN:ParseFormula("(" .. formula:sub(2, -2) .. ")")
end
