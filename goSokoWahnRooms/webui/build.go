// Build-Tool des Frontends: bündelt src/main.ts (samt style.css) mit esbuild
// nach ../web/static/app.js + app.css. Eigenständiges kleines Go-Modul, damit
// die esbuild-Abhängigkeit nicht im Hauptmodul landet (Konzept Kap. 9.2:
// das gebaute Bundle wird eingecheckt, "go build" allein bleibt lauffähig).
//
// Aufruf: bash build.sh  (oder direkt: go run .)
package main

import (
	"fmt"
	"os"

	"github.com/evanw/esbuild/pkg/api"
)

func main() {
	result := api.Build(api.BuildOptions{
		EntryPoints:       []string{"src/main.ts"},
		Outfile:           "../web/static/app.js",
		Bundle:            true,
		Write:             true,
		Target:            api.ES2022,
		MinifyWhitespace:  true,
		MinifySyntax:      true,
		MinifyIdentifiers: true,
		LegalComments:     api.LegalCommentsNone,
		LogLevel:          api.LogLevelInfo,
	})
	if len(result.Errors) > 0 {
		os.Exit(1)
	}

	for _, name := range []string{"../web/static/app.js", "../web/static/app.css"} {
		if info, err := os.Stat(name); err == nil {
			fmt.Printf("%s: %d Bytes\n", name, info.Size())
		}
	}
}
