package tools

import "testing"

func TestFormatInt(t *testing.T) {
	cases := []struct {
		value int64
		want  string
	}{
		{0, "0"},
		{7, "7"},
		{999, "999"},
		{1000, "1.000"},
		{1234, "1.234"},
		{10007, "10.007"},
		{1234567, "1.234.567"},
		{1000000000, "1.000.000.000"},
		{-1234567, "-1.234.567"},
	}
	for _, c := range cases {
		if got := FormatInt(c.value); got != c.want {
			t.Errorf("FormatInt(%d): erwartet %q, erhalten %q", c.value, c.want, got)
		}
	}

	// verschiedene Ganzzahl-Typen ohne Casting
	if got := FormatInt(12345); got != "12.345" { // int
		t.Errorf("int: erhalten %q", got)
	}
	if got := FormatInt(uint16(54321)); got != "54.321" {
		t.Errorf("uint16: erhalten %q", got)
	}
	if got := FormatInt(int8(-128)); got != "-128" {
		t.Errorf("int8: erhalten %q", got)
	}
	if got := FormatInt(uint64(18446744073709551615)); got != "18.446.744.073.709.551.615" {
		t.Errorf("uint64-Maximum: erhalten %q", got)
	}
	if got := FormatInt(int64(-9223372036854775808)); got != "-9.223.372.036.854.775.808" {
		t.Errorf("int64-Minimum: erhalten %q", got)
	}
}
