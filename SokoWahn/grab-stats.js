var a = $$(".catalog-item1");
var n = $(".breadcrumb").innerText.replace("Collections » ", "");
if (n.indexOf("\n") > 0) n = n.substr(0, n.indexOf("\n"));
var s = "\r\n";
for (var i = 0; i < a.length; i++)
{
  if (!a[i].children[1].children[1]) continue;
  var txt = a[i].firstChild.innerText;
  var cc = (a[i].children[1].children[1].firstChild.firstChild.firstChild.lastChild.innerText * 1).toLocaleString().padStart(5);
  s += cc + ": " + n + " :: " + txt + "\r\n";
}
s;
