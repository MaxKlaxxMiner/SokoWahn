package web

import (
	"strings"
	"testing"
)

// Wächter für die "Bundle eingecheckt"-Invariante (Konzept Kap. 9.2): index.html
// referenziert app.js/app.css, beide müssen mit eingebettet sein - sonst wurde nach
// Frontend-Änderungen vergessen, webui/build.sh laufen zu lassen.
func TestStaticBundleEmbedded(t *testing.T) {
	index, err := staticFS.ReadFile("static/index.html")
	if err != nil {
		t.Fatal("index.html fehlt im Embed:", err)
	}
	for _, name := range []string{"app.js", "app.css"} {
		if !strings.Contains(string(index), name) {
			t.Errorf("index.html referenziert %s nicht", name)
		}
		data, err := staticFS.ReadFile("static/" + name)
		if err != nil {
			t.Fatalf("%s fehlt im Embed (webui/build.sh vergessen?): %v", name, err)
		}
		if len(data) == 0 {
			t.Errorf("%s ist leer", name)
		}
	}
}
