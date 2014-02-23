**Hrpg-Rtm Sync.**

Download the Windows 7 executable here:

https://s3-us-west-2.amazonaws.com/hrpg-rtm-sync/v1.01/Hrpg-Rtm-Sync.exe

Released under the GNU GPLv3.0 license.

**Purpose:**

Synchronize your completed Remember the Milk tasks with HabitRPG.

**Description:**

Run Hrpg-Rtm Sync to synchronize all of the RTM tasks that you have marked as "complete" since the previous run of Hrpg-Rtm Sync.
	
The program will create three HRPG habits: "Completed High Difficulty RTM Task," "Completed Medium Difficulty RTM Task," and "Completed Low Difficulty RTM Task." Each time an RTM task is completed, the corresponding HRPG habit tracker will be incremented.
	
To define the difficulty level of an RTM task, add the tag "#d1" for easy, "#d2" for medium, and "#d3" for difficult tasks through the RTM interface, the same way you would add any other tag. Hrpg-Rtm Sync will treat RTM tasks with no difficulty tag as "Easy" tasks.
	
Hrpg-Rtm Sync will record the date and time each time it is run, and will retrieve only the RTM tasks that have been marked as completed since the previous run.

On the initial run, no RTM tasks will be synchronized. In order to synchronize RTM tasks completed prior to the first run on Hrpg-Rtm Sync, manually edit the file hrpg-rtm-sync.cfg and change the "RTM Previous Sync" value to the date of the earliest task to synchronize.

**Operation:**

1. Click the link at the top of this document in order to download the Windows 7 executable.

2. Run the program. Click "Run" when Windows says "the publisher could not be verified."

3. Click "Yes" when prompted to create the config file hrpg-rtm-sync.cfg.

4. Edit hrpg-rtm-sync.cfg and supply your HRPG User ID and API Token. These values are available from HRPG. As of 2014-02-22, the values can be found by clicking on Options > Settings > API.

5. Run the program again. For the initial run, no RTM tasks will be synchronized. Each successive time the program is run, all RTM tasks marked as "completed" since the prior run will be synced with HRPG.

**Build:**

If you want to build from the source files, be sure to add a reference to Newtonsoft's Json.Net.

This can be done in VS2013 Pro as follows:

* Install Newtonsoft Json.Net through NuGet.
* In VS Solution Explorer, right click "References."
* Select the "Extensions" tab.
* Select Json.Net
	
**Privacy Info:**

Hrpg-Rtm Sync does not store or communicate any information about your RTM tasks or HRPG habits with anyone other than RTM and HRPG. It gets a list of your completed RTM tasks from the RTM API and looks for a "#d1" "#d2" or "#d3" tag in each, ignoring everything else, and increments the corresponding HRPG habits. That's it.
	
All communication with HRPG is by HTTPS. Most, but not all communication with RTM is by HTTPS.

**Acknowledgements**

* Lefnire and the rest of the HabitRPG team. https://github.com/HabitRPG.
* The folks at RTM. http://www.rememberthemilk.com.
* WizOneSolutions, who also offers a tool to synchronize HRPG and RTM, available here: https://github.com/wizonesolutions/habitrpg-todo-sync.

**Licensing:**

GNU GPLv3.0: https://www.gnu.org/licenses/gpl.html.
	
**Version History:**

2/22/2014	v1.01

* Added check for blank HRPG User ID, blank HRPG API Token.
* Added text value to form1. (Oops).
* Updates to comments and docs.

2/20/2014	v1.00

* Initial public release.