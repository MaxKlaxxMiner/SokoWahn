// goSokoWahnRooms - Rooms-Framework (Nachbau des C#-Raum-/Portal-Konzepts).
// Konzept siehe docs/konzept.md. Aktueller Stand: M4 (Merge + Deadlock-Scan in der GUI).
//
// Aufruf: goSokoWahnRooms.exe [flags] [level.txt | level-nummer | game-sokoban.com-URL]
// (ohne Level-Argument: eingebautes Level 202, wie das C#-Original; Web-Levels
// landen im geteilten levelcache/-Ordner, derselbe wie bei goSokoWahnBrute)
//
// Standard ist der Debug-GUI-Modus: Webserver starten und Browser öffnen.
// Mit -cli gibt es nur die Kennzahlen auf der Konsole (Verhalten von M1).
package main

import (
	"flag"
	"fmt"
	"net"
	"net/http"
	"os"
	"os/exec"
	"runtime"

	"goSokoWahnRooms/maps"
	"goSokoWahnRooms/rooms"
	"goSokoWahnRooms/soko"
	"goSokoWahnRooms/web"
	"goSokoWahnRooms/weblevel"
)

const defaultAddr = "127.0.0.1:8642"

func main() {
	cli := flag.Bool("cli", false, "nur Kennzahlen ausgeben, kein Webserver")
	addr := flag.String("addr", defaultAddr, "Adresse des Debug-GUI-Servers")
	noBrowser := flag.Bool("nobrowser", false, "Browser nicht automatisch öffnen")
	flag.Parse()

	sokoMap, title := maps.Map5018, "Level 5018: aenigma - soko 47" // aktuelles Arbeits-Level (202 weiter per Argument)
	var webInfo *weblevel.Info
	if arg := flag.Arg(0); arg != "" {
		if weblevel.IsWebInput(arg) {
			level, info, err := weblevel.Load(arg)
			if err != nil {
				fmt.Println("web error:", err)
				os.Exit(1)
			}
			sokoMap, webInfo = level, info
			title = fmt.Sprintf("Level %s: %s - %s (%s)", info.ID, info.Catalog, info.Name, info.Number)
		} else {
			data, err := os.ReadFile(arg)
			if err != nil {
				fmt.Println("read error:", err)
				os.Exit(1)
			}
			sokoMap, title = string(data), arg
		}
	}

	field, err := soko.Parse(sokoMap)
	if err != nil {
		fmt.Println("parse error:", err)
		os.Exit(1)
	}

	network, err := rooms.NewNetwork(field)
	if err != nil {
		fmt.Println("network error:", err)
		os.Exit(1)
	}

	// Level 202: die linke Kammer direkt beim Start zusammenmergen (Max' Arbeitsstand).
	// Die Zahlen sind die Raum-Nummern der GUI ("Room N", 1-basiert): 12,19,25,26,27,
	// 34,35,36,37,47,48,49 - als Indizes also jeweils minus 1.
	if webInfo != nil && webInfo.ID == "202" {
		startMerge := []uint32{11, 18, 24, 25, 26, 33, 34, 35, 36, 46, 47, 48}
		if _, err := network.MergeSelection(startMerge, nil); err != nil {
			fmt.Println("merge error:", err)
			os.Exit(1)
		}
	}

	if webInfo != nil {
		source := "web"
		if webInfo.Cached {
			source = "levelcache"
		}
		fmt.Printf("level %s: %s - %s (%s), quelle: %s\n", webInfo.ID, webInfo.Catalog, webInfo.Name, webInfo.Number, source)
		if webInfo.BestMoves > 0 {
			fmt.Println("bestmoves:", webInfo.BestMoves)
		}
		fmt.Println()
	}
	fmt.Print(field.String())
	fmt.Println()
	states, variants := uint64(0), uint64(0)
	for _, room := range network.Rooms {
		states += room.States.Count()
		variants += room.Variants.Count()
	}
	fmt.Println("rooms:   ", len(network.Rooms))
	fmt.Println("states:  ", states)
	fmt.Println("variants:", variants)
	fmt.Println("effort:  ", network.EffortString())

	if *cli {
		return
	}

	// Wunsch-Adresse belegt (z.B. zweite Instanz)? Dann freien Port nehmen
	listener, err := net.Listen("tcp", *addr)
	if err != nil && *addr == defaultAddr {
		listener, err = net.Listen("tcp", "127.0.0.1:0")
	}
	if err != nil {
		fmt.Println("listen error:", err)
		os.Exit(1)
	}

	url := "http://" + listener.Addr().String()
	fmt.Println()
	fmt.Println("debug-gui:", url)
	if !*noBrowser {
		openBrowser(url)
	}
	if err := http.Serve(listener, web.New(network, title)); err != nil {
		fmt.Println("server error:", err)
		os.Exit(1)
	}
}

// öffnet die URL im Standard-Browser (Fehler egal - die URL steht auf der Konsole)
func openBrowser(url string) {
	var cmd *exec.Cmd
	switch runtime.GOOS {
	case "windows":
		cmd = exec.Command("rundll32", "url.dll,FileProtocolHandler", url)
	case "darwin":
		cmd = exec.Command("open", url)
	default:
		cmd = exec.Command("xdg-open", url)
	}
	_ = cmd.Start()
}
