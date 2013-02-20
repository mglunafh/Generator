open System.Xml
open System.Xml.Linq
open System.IO
open System.Collections
open System.Diagnostics
open System.Threading

System.Console.WriteLine("insert project folder, please")

let path = System.Console.ReadLine()

System.Console.WriteLine("insert project package, please")

let package = System.Console.ReadLine()

// permissions register
let permissions = new Hashtable()

let writeToFile fn (str : string) =
   let conv = System.Text.Encoding.UTF8
   use f = File.CreateText fn
   f.WriteLine(str)

let append (stringBuilder : System.Text.StringBuilder) (str : string) = ignore(stringBuilder.Append(str))
let insert (stringBuilder : System.Text.StringBuilder) position (str : string) = ignore(stringBuilder.Insert(position, str))

let youTubeOverrides = "	 
	@Override
	public void onInitializationFailure(Provider arg0,YouTubeInitializationResult arg1) {
		Toast.makeText(this, \"Initialization Fail\", Toast.LENGTH_LONG).show();
	}
	 
	@Override
	public void onInitializationSuccess(Provider provider, YouTubePlayer player,boolean wasrestored) {
		ytp = player;
		Toast.makeText(this, \"Initialization  Success\", Toast.LENGTH_LONG).show();
	}"

let transformXml (path : string) = 
    let newXml = new System.Text.StringBuilder()

    let reader = XmlReader.Create(path)
    append newXml "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" 
    ignore(reader.Read())
    let mutable currentDepth = 0
    let mutable hasAttr = false
    while (reader.Read()) do
        let depth = reader.Depth

        if hasAttr then
            if (currentDepth < depth) then
                append newXml " >\n"
            else
                append newXml " />\n"

        currentDepth <- depth

        for i in 1..depth do
            append newXml "    "
        let name = reader.Name
        let amount = reader.AttributeCount

        match amount with
        | 0 -> hasAttr <- false
        | _ -> hasAttr <- true

        if name <> "" then 
            if not hasAttr then 
                append newXml ("</" + name + ">" + "\n")
            else
                append newXml ("<" + name + "\n")
        else
            append newXml (name + "\n")

        for i in 0..amount - 1 do
            for i in 0..depth do
                append newXml "    "
            reader.MoveToAttribute(i)
            let name = reader.Name
            match name with
            |"xmlns" ->
                if i = amount - 1 then 
                    append newXml (reader.Name + ":android" + "=" + "\"" + reader.Value + "\"")
                else 
                    append newXml (reader.Name + ":android" + "=" + "\"" + reader.Value + "\"" + "\n")
            |_ ->
                if i = amount - 1 then 
                    append newXml ("android:" + reader.Name + "=" + "\"" + reader.Value + "\"")
                else
                    append newXml ("android:" + reader.Name + "=" + "\"" + reader.Value + "\"" + "\n")
    reader.Close()
    writeToFile path (newXml.ToString())
    

let activities = new System.Text.StringBuilder()

let manifest = new System.Text.StringBuilder()
append manifest ("<?xml version=\"1.0\" encoding=\"utf-8\"?>
<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"
    package=\"com.qrealclouds." + package + "\"
    android:versionCode=\"1\"
    android:versionName=\"1.0\">

    <uses-sdk android:minSdkVersion=\"8\"/>\n")

let createImplementation form =
    if (form = "main") then
        append activities ("        <activity android:name=\".main\"
            android:label=\"@string/app_name\">
            <intent-filter>
                <action android:name=\"android.intent.action.MAIN\"/>
                <category android:name=\"android.intent.category.LAUNCHER\"/>
            </intent-filter>
        </activity>")
    else
        append activities ("\n        <activity android:name=\"." + form + "\"></activity>")

    // imports register
    let imports = new Hashtable()

    let currentImports = new System.Text.StringBuilder()
    append currentImports ("\nimport android.app.Activity;
import android.os.Bundle;
import android.view.View;\n")

    let ifHaveYouTubeElement = false
    let onCreate = new System.Text.StringBuilder()
    append onCreate ("\n\n    /**
    * Called when the activity is first created.
    */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout." + form + ");")
            
    let activity = System.Text.StringBuilder()
    append activity (currentImports.ToString())

    let reader = XmlReader.Create(path + @"\res\layout\" + form + ".xml")
    
    while (reader.Read ()) do 
        match reader.Name with
        |"Button" ->
            append activity ("\npublic class " + form + " extends Activity {")
            let onClickName = reader.GetAttribute("android:onClick")

            if not (imports.ContainsKey("android.content.Intent")) then 
                insert activity 0 "\nimport android.content.Intent;"
                imports.Add("android.content.Intent", "android.content.Intent")

            append activity ("\n\n    public void " + onClickName + "(View v) {")

            let readerTrans = XmlReader.Create(path + @"\Transition2.xml")
            let id = reader.GetAttribute("android:id")

            while ((readerTrans.Read()) && not (readerTrans.GetAttribute("id") = id )) do
                ignore()

            let nextForm = readerTrans.GetAttribute("name_to")

            append activity ("
        Intent intent = new Intent(this, " + nextForm + ".class);
        startActivity(intent);")
            append activity "\n    }"

        |"WebView" ->
            append activity ("\npublic class " + form + " extends Activity {")
            
            if not (permissions.ContainsKey("Internet")) then 
                append manifest "    <uses-permission android:name=\"android.permission.INTERNET\" />\n"
                permissions.Add("Internet", "Internet")

            if not (imports.ContainsKey("android.webkit.WebView")) then 
                insert activity 0 "\nimport android.webkit.WebView;"
                imports.Add("android.webkit.WebView", "android.webkit.WebView")
            
            append onCreate ("\n        WebView webView = (WebView) findViewById(R.id.webview);
        webView.getSettings().setJavaScriptEnabled(true);")

            let readerTrans = XmlReader.Create(path + @"\Transition2.xml")
            let id = reader.GetAttribute("android:id")
            while ((readerTrans.Read()) && not (readerTrans.GetAttribute("id") = id )) do
                ignore()
           
            match readerTrans.GetAttribute("type") with
            |"url" -> 
                let url = readerTrans.GetAttribute("value")
                append onCreate ("\n        webView.loadUrl(\"" + url + "\");")

            |"youtubeview" -> 
                if not (imports.ContainsKey("android.webkit.WebChromeClient")) then 
                    insert activity 0 "\nimport android.webkit.WebChromeClient;"
                    imports.Add("android.webkit.WebChromeClient", "android.webkit.WebChromeClient")
                let code = readerTrans.GetAttribute("value")
                
                append onCreate ("\n		webView.setWebChromeClient(new WebChromeClient() {});
        String html = \"<iframe class=\\\"youtube-player\\\" style=\\\"border: 0; width: 100%; height: 95%; padding:0px; margin:0px\\\" id=\\\"ytplayer\\\" type=\\\"text/html\\\" src=\\\"http://www.youtube.com/embed/" + code + "?fs=0\\\" frameborder=\\\"0\\\">\\\n" + "</iframe>\\\n\";
		webView.getSettings().setPluginsEnabled(true);
	    webView.loadDataWithBaseURL(\"\", html,\"text/html\", \"UTF-8\", \"\");")

            |"googlemapsview" ->
                if not (imports.ContainsKey("android.webkit.WebChromeClient")) then 
                    insert activity 0 "\nimport android.webkit.WebChromeClient;"
                    imports.Add("android.webkit.WebChromeClient", "android.webkit.WebChromeClient")
                append onCreate ("\n		webView.setWebChromeClient(new WebChromeClient() {});
        String html = \\\"<iframe  style=\\\"width: 100%; height: 95%; frameborder=\\\"0\\\" scrolling=\\\"no\\\" marginheight=\\\"0\\\" \\\"marginwidth=\\\"0\\\" src=\\\"https://maps.google.com/maps?ie=UTF8&amp;ll=59.86809,29.862041&amp;spn=0.024215,0.084543&amp;t=h&amp;z=14&amp;output=embed\\\">\\\" + \\\"</iframe><br />\\\";
        webView.loadDataWithBaseURL(\"\", html,\"text/html\", \"UTF-8\", \"\");")
            |_ -> ()

        |"com.google.android.youtube.player.YouTubePlayerView" ->
            let ifHaveYouTubeElement = true
            let id = reader.GetAttribute("android:id")
            let readerTrans = XmlReader.Create(path + @"\Transition2.xml")
            while ((readerTrans.Read()) && not (readerTrans.GetAttribute("id") = id )) do
                ignore()

            let videoCode = readerTrans.GetAttribute("value");

            append activity ("\npublic class " + form + "extends YouTubeBaseActivity 
 	implements YouTubePlayer.OnInitializedListener,OnEditorActionListener {")

            if not (permissions.ContainsKey("Internet")) then 
                append manifest "    <uses-permission android:name=\"android.permission.INTERNET\" />\n"
                permissions.Add("Internet", "Internet")

            let youTubeImports = ["com.google.android.youtube.player.YouTubeBaseActivity";
                                  "com.google.android.youtube.player.YouTubeInitializationResult";
                                  "com.google.android.youtube.player.YouTubePlayer";
                                  "com.google.android.youtube.player.YouTubePlayer.Provider";
                                  "com.google.android.youtube.player.YouTubePlayerView";
                                  "android.widget.Toast"]

            List.iter (fun import -> insert activity 0 ("\nimport " + import + ";")
                                     imports.Add(import, import)) youTubeImports

            append onCreate ("\n        		String developerKey = \"AI39si7GnhmZNIsKDvXYNDZwU0eOlTlWTJZ7zd0JmKoVVhCFzwfmSn6oI0yZDz8edJmDafwYgduM7IqerCF1yofdZlg-m2gluw\";
        YouTubePlayerView youTubePlayerView = (YouTubePlayerView) findViewById(R.id." + id + ");
		youTubePlayerView.initialize(developerKey, this);
        youTubePlayerView.loadVideo(" + videoCode + ")")

        |"com.google.android.maps.MapView" ->
            let id = reader.GetAttribute("android:id")
            let googleMapsImports = ["com.google.android.maps.MapActivity";
                                     "com.google.android.maps.MapView";
                                     ""]
            List.iter (fun import -> insert activity 0 ("\nimport " + import + ";")
                                     imports.Add(import, import) ) googleMapsImports

            append activity ("\npublic class " + form + " extends MapActivity {")
            append onCreate ("\n        MapView mapView = (MapView) findViewById(R.id." + id + ");      
        mapView.setBuiltInZoomControls(true);")

        |_ -> ()

    append onCreate "\n    }"
    append onCreate (if ifHaveYouTubeElement then youTubeOverrides else "")

    append activity (onCreate.ToString())
    append activity "\n}"

    insert activity 0 ("package com.qrealclouds." + package + ";\n")

    writeToFile (path + @"\src\com\qrealclouds\" + package + "\\" + form + ".java") (activity.ToString())

ignore (System.IO.Directory.CreateDirectory(path + @"\src\com\qrealclouds\" + package))

let source = new DirectoryInfo(path + @"\res\layout")
let listOffiles = source.GetFiles()

for i in 0 .. listOffiles.Length - 1 do
    let currentName = listOffiles.[i].Name
    transformXml listOffiles.[i].FullName
    let length = currentName.Length
    createImplementation(currentName.Substring(0, length - 4))

append manifest ("\n    <application android:label=\"@string/app_name\"
        android:theme=\"@android:style/Theme.Light.NoTitleBar\">\n")
append manifest (activities.ToString())
append manifest ("\n    </application>
</manifest>")
writeToFile (path + "\AndroidManifest.xml") (manifest.ToString())

let createApk =
    let createBuildXml = "android update project --target 1 -p " + path
    Thread.Sleep(1000);
    let pathToAndroidSdk = @"D:\android-sdk\sdk\tools\" //для работы на другом компе нужно заменть путь до android sdk
    ignore(System.Diagnostics.Process.Start("cmd.exe", "/C " + pathToAndroidSdk + createBuildXml))
    Thread.Sleep(1000);
    ignore(System.Diagnostics.Process.Start("cmd.exe", "/C " + "cd d" + path + " & ant debug")) 

createApk