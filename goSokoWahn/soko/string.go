package soko

func (f *Field) String() string {
	output := make([]byte, 0, len(f.fieldData)+f.height)

	for y := 0; y < f.height; y++ {
		for x := 0; x < f.width; x++ {
			c := f.fieldData[x+y*f.width]
			switch c {
			case '$', '@':
				c = ' '
			case '*', '+':
				c = '.'
			}
			output = append(output, c)
		}
		output = append(output, '\n')
	}

	px, py := f.wposToField[f.player]%f.width, f.wposToField[f.player]/f.width
	if opos := px + py*(f.width+1); output[opos] == ' ' {
		output[opos] = '@'
	} else {
		output[opos] = '+'
	}
	for _, box := range f.boxes {
		px, py := f.wposToField[box]%f.width, f.wposToField[box]/f.width
		if opos := px + py*(f.width+1); output[opos] == ' ' {
			output[opos] = '$'
		} else {
			output[opos] = '*'
		}
	}

	return string(output)
}
