-- @author Malody Hoe / GitHub: madhoe / Twitter: maddhoexD
-- Script to generate the image meters used to display badges and emotes.
-- Thanks to Malody Hoe, the author behind Monstercat Visualizer! This
-- script is heavily based on his/her work.

function Initialize()
	local numEmotes = SELF:GetNumberOption("NumImages")
	local file = io.open(SKIN:MakePathAbsolute(SELF:GetOption("IncFile")), "w")
	
	local t = {}
	
	local names = {'Name', 'X', 'Y'}
	for i = 0, numEmotes-1 do
		for nameCount = 1, #names do
			local n = names[nameCount]
			local name = "TwitchImage" .. n .. "%%"
			table.insert(t, "[" .. insertNumber(name, i) .. "]")
			insertOptions("Twitch", t, i)
			table.insert(t, "Type=" ..insertNumber(name, i))
			table.insert(t, "")
		end
		table.insert(t, "[" .. insertNumber("TwitchImage%%", i) .. "]")
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
		table.insert(t, opt .. "=" .. insertNumber(SELF:GetOption( op .. "Value" .. j), i))
		j = j + 1
	end
end	

function insertNumber(value, i)
	return value:gsub("%%%%", i)
end
