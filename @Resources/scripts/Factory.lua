-- @author Tim Lindeberg / GitHub: timlindeberg
-- Script to generate the image meters used to display badges and emotes.
-- Thanks to Malody Hoe, the author behind Monstercat Visualizer!
-- This script is heavily based on his/her work.

function Initialize()
	local numImages = SELF:GetNumberOption("NumImages")
	local numLinks = SELF:GetNumberOption("NumLinks")
	local numGifs = SELF:GetNumberOption("NumGifs")
	local file = io.open(SKIN:MakePathAbsolute(SELF:GetOption("IncFile")), "w")
	
	local t = {}
	
	local imageVars = {'Name', 'X', 'Y', 'ToolTip'}
	local gifVars = {'Name', 'X', 'Y', 'ToolTip'}
	local linkVars = {'Name', 'X', 'Y', 'Width', 'Height'}
	insert(t, "Image", imageVars, numImages)
	insert(t, "Gif", gifVars, numGifs)
	insert(t, "Link", linkVars, numLinks)

	file:write(table.concat(t, "\n"))
	file:close()
end

function insert(t, tpe, vars, num)
	for i = 0, num-1 do
		for count = 1, #vars do
			local v = vars[count]
			local name = tpe .. v .. "%%"
			table.insert(t, "[" .. insertNumber(name, i) .. "]")
			insertOptions("Twitch", t, i)
			insertOptions(tpe .. v, t, i)
			table.insert(t, "Type=" ..insertNumber(name, i))
			table.insert(t, "")
		end
		table.insert(t, "[" .. insertNumber(tpe .. "%%", i) .. "]")
		insertOptions(tpe, t, i)
		table.insert(t, "\n;------------------------------------------------;\n")
	end
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
