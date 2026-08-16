# GameTimers

> **GameTimers** – A lightweight Windows application to manage multiple concurrent timers. Perfect for keeping track of game tasks (e.g., *Supermarket Village*) or daily activities. Features priority tagging, custom remarks, flexible time inputs (e.g., `45m`, `1:30`), and notification pop-ups when timers expire.

---

## Dutch / Nederlands

### Timers die je helpen op tijd actie te nemen
Heb jij ook behoefte aan het aanmaken van verschillende timers om op tijd zaken af te handelen? Dan zou GameTimers wel eens iets voor jou kunnen zijn.

Alles begint met het aanmaken van de timers die je nodig hebt. Klik op het **+** teken rechtsboven in de app om een nieuwe timer aan te maken. Maak er een aantal aan om direct te starten.

Je ziet je timers vervolgens in de lijst verschijnen. Een nieuwe timer wordt meteen actief. In de lijst zie je de resterende tijd teruglopen naar 0, zodat je op tijd actie kunt ondernemen. Afhankelijk van de vervolgactie die je wilt uitvoeren, klik je met de rechtermuisknop op een timer en kies je de gewenste optie.

### Kenmerken van een timer invoeren
Bij het aanmaken of bewerken van een timer geef je als eerste een naam in. Vervolgens kun je tijden op diverse manieren invoeren:
* `45` → 45 minuten
* `1:30` of `01:30` → 1 uur en 30 minuten
* `45 min` / `45m` → 45 minuten
* `2 uur` / `2u` → 2 uur
* `90 sec` / `90s` → 90 seconden

Je kunt bij iedere timer ook aangeven dat deze **prioriteit** heeft. De timer krijgt dan een opvallende gele markering in de statuskolom en je kunt hier specifiek op filteren.

Daarnaast kun je een **opmerking** toevoegen voor extra verduidelijking over wat jouw bedoeling is met de timer.

### Acties naar aanleiding van een timer
* **Dubbelklikken:** Pas de gegevens van een timer aan.
* **Rechtermuisknop:**
  * **Stop:** Pauzeert de lopende timer.
  * **Opnieuw:** Herstart de timer direct vanaf de oorspronkelijke ingestelde tijd.
  * **Afgehandeld:** Markeer de timer als verwerkt. Als de timer prioriteit heeft, wordt gevraagd of deze prioriteit mag vervallen. De timer komt vervolgens zonder resterende tijd onderaan de lijst te staan.
  * **Aanpassen:** Wijzig de instellingen van de timer.
  * **Verwijder:** Verwijder de timer definitief uit de lijst.

### Knoppen boven de lijst met Timers
* **Filter:** Typ in het zoekveld om snel op naam te zoeken (hoofdletterongevoelig).
* **Prioriteit-vinkje:** Toon alleen timers die gemarkeerd zijn als prioriteit.
* **Wis-knop (Kruisje):** Verschijnt zodra er een filter of vinkje actief is en verwijdert de zoekcriteria zodat alle timers weer zichtbaar zijn.
* **+ Knop:** Maak een nieuwe timer aan.
* **Tandwiel Knop:** Open de instellingen.
* **? Knop:** Opent deze handleiding in je browser.

### Instellingen
Via het tandwiel-icoon open je het instellingenscherm met de volgende opties:
* **Taal:** Schakel tussen Automatisch (Systeemtaal), Nederlands of Engels.
* **Pop-up bij afgeronde timers:** Schakelt een groot, opvallend venster in dat verschijnt zodra een timer afloopt.
* **XML openen:** Opents het bestand `GameTimers.xml` direct in je standaard tekstverwerker om gegevens in te zien of te bewerken.
* **Map openen:** Opents de map in Windows Verkenner waarin jouw gegevens en instellingen staan opgeslagen.

### Opstartparameters
`GameTimers.exe` kan los gestart worden vanuit iedere map. Het heeft geen verdere installatie nodig. Het kan gestart worden met optionele opstartparameters (command-line arguments) om de map en titelbalk in te stellen:
* **Parameter 1:** Map waar `GameTimers.xml` opgeslagen wordt.
* **Parameter 2:** Naam van het spel of de lijst voor in de titelbalk.

#### Voorbeelden van gebruik:
```cmd
GameTimers.exe
GameTimers.exe "C:\MijnData"
GameTimers.exe "C:\MijnData" "Supermarket Village"
```

### Aanleiding
De aanleiding voor het maken van deze app was het spelen van het spel *Supermarket Village* via Google Play Games op een PC. Bij dit soort spellen moet je veel processen gelijktijdig in de gaten houden. Zodra een gewas klaar is of een product gemaakt is, wil je direct de volgende stap in gang zetten. Omdat een geschikte standaard app ontbrak, is GameTimers ontwikkeld om eenvoudig overzicht en controle te houden.


---

## English

### Timers that help you take action on time
Do you also need to set up multiple timers to handle tasks on time? Then GameTimers might be just what you need.

It all starts with creating the timers you need. Click the **+** icon at the top right of the app to create a new timer. Create a few to get started right away.

You will then see your timers appear in the list. A new timer becomes active immediately. In the list, you will see the remaining time counting down to 0 so you can take timely action. Depending on the follow-up action you want to perform, right-click a timer and choose the desired option.

### Entering timer properties
When creating or editing a timer, first enter a name. Then you can input durations in various ways:
* `45` → 45 minutes
* `1:30` or `01:30` → 1 hour and 30 minutes
* `45 min` / `45m` → 45 minutes
* `2 hours` / `2h` → 2 hours
* `90 sec` / `90s` → 90 seconds

For each timer, you can also mark it as **priority**. The timer will receive a prominent yellow highlight in the status column, and you can specifically filter on it.

Additionally, you can add a **remark** to clarify your intention for the timer.

### Actions on a timer
* **Double-clicking:** Edit timer details.
* **Right-click:**
  * **Stop:** Pauses the running timer.
  * **Restart:** Restarts the timer immediately from its original configured time.
  * **Handled:** Mark the timer as completed. If the timer has priority, you will be asked if the priority should be removed. The timer is then placed at the bottom of the list without remaining time.
  * **Edit:** Change the settings of the timer.
  * **Delete:** Permanently delete the timer from the list.

### Buttons above the Timers list
* **Filter:** Type in the search field to quickly filter by name (case-insensitive).
* **Priority Checkbox:** Show only timers marked with high priority.
* **Clear Button (Cross):** Appears as soon as a filter or checkbox is active, clearing search criteria so all timers are visible again.
* **+ Button:** Create a new timer.
* **Gear Button:** Open settings.
* **? Button:** Opens this manual in your browser.

### Settings
Open the settings dialog via the gear icon to access the following options:
* **Language:** Switch between Automatic (System default), Nederlands, or English.
* **Pop-up for completed timers:** Enables a large, prominent window that appears as soon as a timer finishes.
* **Open XML:** Opens the `GameTimers.xml` file directly in your default text editor to inspect or edit raw data.
* **Open Folder:** Opens the folder in Windows Explorer where your data and settings are saved.

### Startup Parameters
`GameTimers.exe` can be launched stand-alone from any folder. It requires no installation. It can be started with optional command-line arguments to specify where data should be stored and what title should be shown in the title bar:
* **Parameter 1:** Folder where `GameTimers.xml` is saved.
* **Parameter 2:** Name of the game or list to display in the title bar.

#### Usage Examples:
```cmd
GameTimers.exe
GameTimers.exe "C:\MyData"
GameTimers.exe "C:\MyData" "Supermarket Village"
```

### Background
The motivation for creating this app was playing the game *Supermarket Village* via Google Play Games on PC. In these types of games, you need to keep track of many concurrent processes. As soon as a crop is ready or a product is crafted, you want to initiate the next step right away. Because a suitable standard app was lacking, GameTimers was built to keep clear overview and control.

---

## License / Author
Developed by **John**.
Feel free to contribute, report issues, or suggest new features!

<img width="1400" height="783" alt="afbeelding" src="https://github.com/user-attachments/assets/457ac9cb-3586-4e36-ac1b-86c41f5202bf" />

<img width="1387" height="774" alt="afbeelding" src="https://github.com/user-attachments/assets/d390d990-6669-479a-a11b-d620b91dd768" />

