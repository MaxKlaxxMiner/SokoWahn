// Wegwerf-Sonde: merged die linke Kammer von Level 202 (FieldTest4) wie der
// Go-Port und dumpt Zustände/Varianten des gemergten Raums zum Vergleich
using System;
using System.Linq;
using SokoWahnLib;
using SokoWahnLib.Rooms;

static class RoomsProbe
{
  static readonly SokoField Field202 = new SokoField(@"
       #####
      ##   #
      #    #
    ###    ######
    #.#.# ##.   #
  ### ###  ##   #
  #   #  $  ## ##
  #     $@$     #
  #   #  $  #   #
  ######   ### ##
   #  .## #### #
   #           #
   ##  #########
    ####
  ");

  static Room RoomWithField(RoomNetwork network, int fieldPos)
  {
    return network.rooms.First(r => r.fieldPosis.Contains(fieldPos));
  }

  static void Main()
  {
    var network = new RoomNetwork(Field202);
    int width = network.field.Width;

    // Wpos aus dem Go-Port -> Feldpositionen: begehbare Felder zählen
    var walkPosis = network.field.GetWalkPosis().OrderBy(x => x).ToArray();
    int[] wpos = { 11, 18, 24, 25, 26, 33, 34, 35, 36, 46, 47, 48 };
    var fields = wpos.Select(w => walkPosis[w]).ToArray();

    // MergeSelection wie im Go-Port: solange zwei ausgewählte Räume direkt
    // verbunden sind, wird paarweise gemergt
    for (; ; )
    {
      var selected = fields.Select(f => RoomWithField(network, f)).Distinct().ToArray();
      if (selected.Length == 1) break;
      Room a = null, b = null;
      foreach (var r in selected)
      {
        foreach (var op in r.outgoingPortals)
        {
          if (!ReferenceEquals(op.toRoom, r) && selected.Contains(op.toRoom)) { a = r; b = op.toRoom; break; }
        }
        if (a != null) break;
      }
      if (a == null) throw new Exception("selection not connected");
      network.MergeRooms(a, b);
    }
    var blob = RoomWithField(network, fields[0]);
    network.Validate(true);

    Console.WriteLine("rooms: " + network.rooms.Length);
    Console.WriteLine("merged: fields=" + blob.fieldPosis.Length + " states=" + blob.stateList.Count +
                      " variants=" + blob.variantList.Count + " startState=" + blob.startState);
    for (ulong id = 0; id < blob.stateList.Count; id++)
    {
      var boxes = blob.stateList.Get(id);
      Console.WriteLine("state " + id + ": " + string.Join(",", boxes.Select(b => (b % width) + "/" + (b / width))));
    }
    foreach (var portal in blob.incomingPortals)
    {
      Console.WriteLine("portal " + portal.iPortalIndex + ": " + (portal.fromPos % width) + "/" + (portal.fromPos / width) +
                        " -> " + (portal.toPos % width) + "/" + (portal.toPos / width));
      foreach (var state in portal.variantStateDict.GetAllStates().OrderBy(x => x))
      {
        foreach (var v in portal.variantStateDict.GetVariantSpan(state).AsEnumerable())
        {
          var d = blob.variantList.GetData(v);
          Console.WriteLine("  state " + state + " variant " + v + ": new=" + d.newState + " moves=" + d.moves +
                            " pushes=" + d.pushes + " boxes=[" + string.Join(",", d.oPortalIndexBoxes) + "] exit=" +
                            (d.oPortalIndexPlayer == uint.MaxValue ? "END" : d.oPortalIndexPlayer.ToString()) + " path=" + d.path);
        }
      }
      foreach (var key in portal.stateBoxSwap.GetAllKeys().OrderBy(x => x))
      {
        Console.WriteLine("  boxswap " + key + " -> " + portal.stateBoxSwap.Get(key));
      }
    }
  }
}
