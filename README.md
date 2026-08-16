# GameTimers
        <h2>Timers that help you take action on time</h2>
        <p>
            Do you also need to set up multiple timers to handle tasks on time?
            Then GameTimers might be just what you need.<br>
            It all starts with creating the timers you need. 
            Click the <strong>+</strong> icon at the top right of the app to create a new timer. 
            Create a few to get started right away.<br>
            You will then see your timers appear in the list. A new timer becomes active immediately. 
            In the list, you will see the remaining time counting down to 0 so you can take timely action. 
            Depending on the follow-up action you want to perform, right-click a timer and choose the desired option.
        </p>

        <h2>Entering timer properties</h2>
        <p>When creating or editing a timer, first enter a name. Then you can input durations in various ways:</p>
        <ul>
            <li><code>45</code> &rarr; 45 minutes</li>
            <li><code>1:30</code> or <code>01:30</code> &rarr; 1 hour and 30 minutes</li>
            <li><code>45 min</code> / <code>45m</code> &rarr; 45 minutes</li>
            <li><code>2 hours</code> / <code>2h</code> &rarr; 2 hours</li>
            <li><code>90 sec</code> / <code>90s</code> &rarr; 90 seconds</li>
        </ul>
        <p>
            For each timer, you can also mark it as <strong>priority</strong>. The timer will receive a prominent yellow highlight in the status column, and you can specifically filter on it.<br>
            Additionally, you can add a <strong>remark</strong> to clarify your intention for the timer.
        </p>

        <h2>Actions on a timer</h2>
        <p>
            <strong>Double-clicking</strong> a timer allows you to edit its details.<br>
            <strong>Right-click</strong> a timer in the list for the following options:
        </p>
        <ul>
            <li><strong>Stop:</strong> Pauses the running timer.</li>
            <li><strong>Restart:</strong> Restarts the timer immediately from its original configured time.</li>
            <li><strong>Handled:</strong> Mark the timer as completed. If the timer has priority, you will be asked if the priority should be removed. The timer is then placed at the bottom of the list without remaining time.</li>
            <li><strong>Edit:</strong> Change the settings of the timer.</li>
            <li><strong>Delete:</strong> Permanently delete the timer from the list.</li>
        </ul>

        <h2>Buttons above the Timers list</h2>
        <ul>
            <li><strong>Filter:</strong> Type in the search field to quickly filter by name (case-insensitive).</li>
            <li><strong>Priority Checkbox:</strong> Show only timers marked with high priority.</li>
            <li><strong>Clear Button (Cross):</strong> Appears as soon as a filter or checkbox is active, clearing search criteria so all timers are visible again.</li>
            <li><strong>+ Button:</strong> Create a new timer.</li>
            <li><strong>Gear Button:</strong> Open settings.</li>
            <li><strong>? Button:</strong> Opens this manual in your browser.</li>
        </ul>

        <h2>Settings</h2>
        <p>Open the settings dialog via the gear icon to access the following options:</p>
        <ul>
            <li><strong>Language:</strong> Switch between Automatic (System default), Nederlands, or English.</li>
            <li><strong>Pop-up for completed timers:</strong> Enables a large, prominent window that appears as soon as a timer finishes.</li>
            <li><strong>Open XML:</strong> Opens the <code>GameTimers.xml</code> file directly in your default text editor to inspect or edit raw data.</li>
            <li><strong>Open Folder:</strong> Opens the folder in Windows Explorer where your data and settings are saved.</li>
        </ul>

        <h2>Startup Parameters</h2>
        <p>
            GameTimers.exe can be launched stand-alone from any folder.
            It requires no installation.
            It can be started with optional command-line arguments to specify where data should be stored and what title should be shown in the title bar:
        </p>
        <ul>
            <li><code>Parameter 1:</code> Folder where <code>GameTimers.xml</code> is saved.</li>
            <li><code>Parameter 2:</code> Name of the game or list to display in the title bar.</li>
        </ul>
        <h3>Usage Examples</h3>
        <ul>
            <li><code>GameTimers.exe</code></li>
            <li><code>GameTimers.exe "C:\MyData"</code></li>
            <li><code>GameTimers.exe "C:\MyData" "Supermarket Village"</code></li>
        </ul>

        <h2>Background</h2>
        <p>
            The motivation for creating this app was playing the game <em>Supermarket Village</em> via <a href="https://play.google.com/googleplaygames/exploregames" target="_blank" rel="noopener noreferrer">Google Play Games</a> on PC. 
            In these types of games, you need to keep track of many concurrent processes. As soon as a crop is ready or a product is crafted, you want to initiate the next step right away. 
            Because a suitable standard app was lacking, GameTimers was built to keep clear overview and control.
        </p>

        <h2>Customization and Collaboration</h2>
        <p>
            If you need customizations, you can incorporate them into the source code yourself.
            We can also collaborate to create a new version that improves upon this one.<br>
            Leave a message on GitHub and we can explore useful additions.<br>
            John
        </p>
