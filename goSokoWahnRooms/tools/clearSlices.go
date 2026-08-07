package tools

// setzt alle Elemente auf false (das builtin clear nutzt intern memclr und ist damit maximal schnell)
func ClearBools(bools []bool) {
	clear(bools)
}
