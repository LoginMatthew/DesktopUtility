namespace DesktopUtility.Localization;

internal sealed class Language
{
    private static readonly string[] Codes = ["en", "hu", "de", "ja"];
    public static readonly string[] Names = ["English", "Magyar", "Deutsch", "日本語"];

    private static readonly Dictionary<string, string[]> Text = new()
    {
        ["Gallery"] = ["Gallery", "Galéria", "Galerie", "ギャラリー"],
        ["Settings"] = ["Settings", "Beállítások", "Einstellungen", "設定"],
        ["About"] = ["About", "Névjegy", "Über die App", "このアプリについて"],
        ["Folder"] = ["Choose folder", "Mappa kiválasztása", "Ordner auswählen", "フォルダーを選択"],
        ["Refresh"] = ["Refresh", "Frissítés", "Aktualisieren", "更新"],
        ["Rotate"] = ["Automatic rotation", "Automatikus váltás", "Automatischer Wechsel", "自動切り替え"],
        ["Selected"] = ["Rotate selected images only", "Csak a kijelölt képek váltása", "Nur ausgewählte Bilder wechseln", "選択した画像のみ切り替え"],
        ["SelectAllImages"] = ["Select all images", "Összes kép kijelölése", "Alle Bilder auswählen", "すべての画像を選択"],
        ["ClearSelectedImages"] = ["Clear selection", "Kijelölés törlése", "Auswahl aufheben", "選択を解除"],
        ["MonitorAssignments"] = ["Assign images by monitor", "Képek hozzárendelése monitoronként", "Bilder pro Monitor zuweisen", "モニターごとに画像を割り当てる"],
        ["MonitorAssignmentHelp"] = ["Select a gallery image, then assign it to a monitor.", "Válassz képet a galériában, majd rendeld hozzá egy monitorhoz.", "Wähle ein Galeriebild und weise es einem Monitor zu.", "ギャラリーの画像を選び、モニターに割り当てます。"],
        ["ApplyMonitorAssignments"] = ["Apply assigned wallpapers", "Hozzárendelt háttérképek alkalmazása", "Zugewiesene Hintergrundbilder anwenden", "割り当てた壁紙を適用"],
        ["AssignSelectedImage"] = ["Assign selected image", "Kiválasztott kép hozzárendelése", "Ausgewähltes Bild zuweisen", "選択した画像を割り当てる"],
        ["ClearAssignment"] = ["Remove assignment", "Hozzárendelés törlése", "Zuweisung entfernen", "割り当てを解除"],
        ["NoAssignedImage"] = ["No image assigned", "Nincs hozzárendelt kép", "Kein Bild zugewiesen", "画像が割り当てられていません"],
        ["LoadingMonitors"] = ["Detecting monitors…", "Monitorok keresése…", "Monitore werden erkannt…", "モニターを検出中…"],
        ["NoMonitors"] = ["No individual monitors available. Refresh the gallery to retry.", "Egyetlen monitor sem érhető el külön. Újrapróbáláshoz frissítsd a galériát.", "Keine einzelnen Monitore verfügbar. Aktualisiere die Galerie, um es erneut zu versuchen.", "個別に設定できるモニターがありません。ギャラリーを更新して再試行してください。"],
        ["Include"] = ["Include in rotation", "Váltásra kijelölve", "Für Wechsel auswählen", "切り替え対象にする"],
        ["Separate"] = ["Different image per monitor", "Monitoronként eltérő kép", "Unterschiedliche Bilder je Monitor", "モニターごとに異なる画像"],
        ["All"] = ["All monitors", "Minden monitor", "Alle Monitore", "すべてのモニター"],
        ["Apply"] = ["Apply now", "Beállítás most", "Jetzt anwenden", "今すぐ適用"],
        ["Favorite"] = ["Set favorite", "Kedvenc jelölése", "Als Favorit speichern", "お気に入りに登録"],
        ["ApplyFavorite"] = ["Apply favorite", "Kedvenc beállítása", "Favorit anwenden", "お気に入りを適用"],
        ["NoFavorite"] = ["Choose a favorite in Gallery.", "Jelölj ki egy kedvencet a Galériában.", "Wähle einen Favoriten in der Galerie.", "ギャラリーでお気に入りを選んでください。"],
        ["Focus"] = ["Select an image to apply it.", "Válassz ki egy képet a beállításhoz.", "Wähle ein Bild zum Anwenden aus.", "適用する画像を選んでください。"],
        ["Empty"] = ["Choose a folder with JPG, PNG or BMP images.", "Válassz JPG, PNG vagy BMP képeket tartalmazó mappát.", "Wähle einen Ordner mit JPG-, PNG- oder BMP-Bildern.", "JPG、PNG、BMP 画像のあるフォルダーを選んでください。"],
        ["Loading"] = ["Reading images…", "Képek beolvasása…", "Bilder werden eingelesen…", "画像を読み込み中…"],
        ["Minutes"] = ["Interval in minutes", "Időköz percben", "Intervall in Minuten", "間隔（分）"],
        ["Save"] = ["Apply interval", "Időköz mentése", "Intervall übernehmen", "間隔を適用"],
        ["BadInterval"] = ["Enter 1–1440 minutes.", "Adj meg 1–1440 percet.", "Gib 1–1440 Minuten ein.", "1～1440 分を入力してください。"],
        ["Next"] = ["Next change", "Következő váltás", "Nächster Wechsel", "次の切り替え"],
        ["Paused"] = ["Rotation paused", "A váltás szünetel", "Wechsel pausiert", "切り替え停止中"],
        ["NoCandidates"] = ["No images for rotation", "Nincs váltásra kijelölt kép", "Keine Bilder für den Wechsel", "切り替え対象の画像がありません"],
        ["Language"] = ["Language", "Nyelv", "Sprache", "言語"],
        ["Accent"] = ["Accent color", "Kiemelőszín", "Akzentfarbe", "アクセントカラー"],
        ["Text"] = ["Text color", "Betűszín", "Textfarbe", "文字色"],
        ["Appearance"] = ["Appearance", "Megjelenés", "Darstellung", "外観"],
        ["Surface"] = ["Surface color", "Felület színe", "Oberflächenfarbe", "背景色"],
        ["PanelOpacity"] = ["Content panel opacity", "Tartalompanel fedettsége", "Deckkraft des Inhaltsbereichs", "コンテンツパネルの不透明度"],
        ["Minimize"] = ["Minimize", "Kis méret", "Minimieren", "最小化"],
        ["Maximize"] = ["Maximize or restore", "Teljes méret vagy visszaállítás", "Maximieren oder wiederherstellen", "最大化または元のサイズ"],
        ["Close"] = ["Close to tray", "Bezárás a tálcára", "In den Infobereich schließen", "通知領域に閉じる"],
        ["Opacity"] = ["Window background opacity", "Ablakháttér fedettsége", "Deckkraft des Fensterhintergrunds", "ウィンドウ背景の不透明度"],
        ["Reset"] = ["Reset appearance", "Megjelenés visszaállítása", "Darstellung zurücksetzen", "外観をリセット"],
        ["Startup"] = ["Start at Windows sign-in", "Indítás Windows-bejelentkezéskor", "Bei Windows-Anmeldung starten", "Windows サインイン時に起動"],
        ["Open"] = ["Open Desktop Utility", "Desktop Utility megnyitása", "Desktop Utility öffnen", "Desktop Utility を開く"],
        ["Exit"] = ["Exit", "Kilépés", "Beenden", "終了"],
        ["PreviewLoading"] = ["Reading details…", "Képadatok beolvasása…", "Bilddaten werden gelesen…", "画像情報を読み込み中…"],
        ["UnknownResolution"] = ["Resolution unavailable", "Felbontás nem elérhető", "Auflösung nicht verfügbar", "解像度を取得できません"],
        ["UnknownFileSize"] = ["File size unavailable", "Fájlméret nem elérhető", "Dateigröße nicht verfügbar", "ファイルサイズを取得できません"],
        ["Developer"] = ["Developed by", "Fejlesztő", "Entwickelt von", "開発者"],
        ["IconCredit"] = [
            "The application icon was created with AI.",
            "Az alkalmazás ikonja mesterséges intelligencia segítségével készült.",
            "Das App-Symbol wurde mit KI-Unterstützung erstellt.",
            "アプリのアイコンは AI の支援で作成されました。"],
        ["AboutDescription"] = [
            "Desktop Utility rotates desktop wallpapers from a folder on Windows. You can use one image across all monitors or assign different images to each display.",
            "A Desktop Utility egy kiválasztott mappából váltja a Windows asztali háttérképeit. Az összes monitorra ugyanazt a képet állíthatod be, vagy kijelzőnként eltérőt választhatsz.",
            "Desktop Utility wechselt Windows-Desktop-Hintergründe aus einem Ordner. Du kannst dasselbe Bild auf allen Monitoren verwenden oder jedem Bildschirm ein eigenes Bild zuweisen.",
            "Desktop Utility は、指定したフォルダーから Windows の壁紙を切り替えます。すべてのモニターで同じ画像を使うことも、画面ごとに別の画像を設定することもできます。"],
        ["Usage"] = ["How to use", "Használat", "Bedienung", "使い方"],
        ["UsageFolder"] = [
            "In Gallery, choose a folder with JPEG, PNG or BMP images. Each thumbnail shows the original pixel dimensions and the file size.",
            "A Galériában válassz JPEG, PNG vagy BMP képeket tartalmazó mappát. Minden miniatűr alatt látható a kép eredeti felbontása és a fájlméret.",
            "Wähle in der Galerie einen Ordner mit JPEG-, PNG- oder BMP-Bildern. Unter jeder Vorschau stehen die ursprünglichen Pixelmaße und die Dateigröße.",
            "ギャラリーで JPEG、PNG、BMP 画像のあるフォルダーを選びます。各サムネイルには元の解像度とファイルサイズが表示されます。"],
        ["UsageApply"] = [
            "Select an image, choose a monitor and click Apply now. Use Set favorite to save an image for quick access.",
            "Jelölj ki egy képet, válassz monitort, majd kattints a Beállítás most gombra. A Kedvenc jelölése gombbal elmentheted a képet a gyors eléréshez.",
            "Wähle ein Bild und einen Monitor aus und klicke auf Jetzt anwenden. Über Als Favorit speichern legst du ein Bild für den Schnellzugriff fest.",
            "画像とモニターを選び、［今すぐ適用］を押します。［お気に入りに登録］で画像をすぐに使えるように保存できます。"],
        ["UsageRotation"] = [
            "In Settings, enable automatic rotation and set the interval. To rotate only certain images, check them in Gallery and enable selected-only rotation.",
            "A Beállításokban kapcsold be az automatikus váltást és állítsd be az időközt. Ha csak bizonyos képeket szeretnél váltani, pipáld ki őket a Galériában, és kapcsold be a Csak a kijelölt képek váltása opciót.",
            "Aktiviere in den Einstellungen den automatischen Wechsel und lege das Intervall fest. Für eine Bildauswahl markiere die gewünschten Bilder in der Galerie und aktiviere den Wechsel nur für ausgewählte Bilder.",
            "設定で自動切り替えを有効にし、間隔を指定します。一部の画像だけを使う場合は、ギャラリーで画像にチェックを入れ、選択した画像のみの切り替えを有効にします。"],
        ["UsageTray"] = [
            "Right-click the icon next to the Windows clock to pause or resume rotation, change the interval, or apply your favorite. Closing the main window leaves the app running in the tray.",
            "Az óra melletti ikonra jobb gombbal kattintva szüneteltetheted vagy folytathatod a váltást, módosíthatod az időközt, vagy beállíthatod a kedvencet. A főablak bezárása után az alkalmazás tovább fut a tálcán.",
            "Klicke mit der rechten Maustaste auf das Symbol neben der Windows-Uhr, um den Wechsel zu pausieren oder fortzusetzen, das Intervall zu ändern oder den Favoriten anzuwenden. Beim Schließen des Hauptfensters läuft die App im Infobereich weiter.",
            "Windows の時計の横にあるアイコンを右クリックすると、切り替えの一時停止・再開、間隔の変更、お気に入りの適用ができます。メインウィンドウを閉じても、アプリは通知領域で動作します。"],
        ["Version"] = ["Version", "Verzió", "Version", "バージョン"],
        ["FileError"] = ["The image folder could not be read.", "A képmappa nem olvasható.", "Der Bildordner konnte nicht gelesen werden.", "画像フォルダーを読み込めませんでした。"],
        ["WallpaperError"] = ["One or more wallpapers could not be applied.", "Egy vagy több háttérképet nem sikerült beállítani.", "Mindestens ein Hintergrundbild konnte nicht angewendet werden.", "壁紙の一部または全部を適用できませんでした。"],
        ["SettingsError"] = ["Settings could not be saved or read.", "A beállításokat nem sikerült olvasni vagy menteni.", "Einstellungen konnten nicht gelesen oder gespeichert werden.", "設定を読み込みまたは保存できませんでした。"],
        ["StartupError"] = ["Windows sign-in settings could not be changed.", "A Windows automatikus indítása nem módosítható.", "Die Windows-Autostarteinstellung konnte nicht geändert werden.", "Windows の自動起動設定を変更できませんでした。"],
        ["PreviousPage"] = ["Previous page", "Előző oldal", "Vorherige Seite", "前のページ"],
        ["NextPage"] = ["Next page", "Következő oldal", "Nächste Seite", "次のページ"]
    };

    public int Index { get; private set; }
    public string Code => Codes[Index];
    public string this[string key] => Text[key][Index];

    public void Select(string code) => Select(Array.IndexOf(Codes, code));
    public void Select(int index) => Index = Math.Clamp(index, 0, Codes.Length - 1);
}
