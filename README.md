***Hrpg-Rtm Sync.***

Released under the GNU GPLv3.0.

**Purpose:**

Synchronize your completed Remember the Milk tasks with HabitRPG.

**Description:**

Run Hrpg-Rtm Sync to synchronize all of the RTM tasks that you have marked as "complete" since the previous run of Hrpg-Rtm Sync.
	
The program will create three HRPG habits: "Completed High Difficulty RTM Task," "Completed Medium Difficulty RTM Task," and
"Completed Low Difficulty RTM Task." Each time an RTM task is completed, the corresponding HRPG habit tracker will be incremented.
	
To define the difficulty level of an RTM task, add the tag "#d1" for easy, "#d2" for medium, and "#d3" for difficult tasks to the
task through RTM, the same way you would add any other tag. Hrpg-Rtm Sync will treat RTM tasks with no difficulty tag as "Easy" tasks.
	
Hrpg-Rtm Sync will record the date and time each time it is run, and will retrieve only the RTM tasks that have been marked as
completed since the previous run.

**Operation:**

In order to use Hrpg-Rtm Sync, complete the following steps:
	
1. Download and compile with C#. Hrpg-Rtm Sync was written using the VS2013 Pro IDE.
	* Be sure to add a project reference to Json.Net.
	* Install NewtonSoft Json.Net through NuGet.
	* In VS Solution Explorer, right click "References."
	* Select the "Extensions" tab.
	* Select Json.Net

2. Edit hrpg-rtm-sync.cfg
	* Add your HRPG "User ID" and "API Token."
		* These values are available through the options menu at HabitRPG.

3. Run the program and click "Sync."

**Privacy Info:**

Hrpg-Rtm Sync does not store or communicate any information about your RTM tasks or HRPG habits with anyone other than RTM and HRPG. It gets a list of your completed RTM tasks from the RTM API and looks for a "#d1" "#d2" or "#d3" tag in each, ignoring everything else, and increments the corresponding HRPG habits. That's it.
	
All communication with HRPG is by HTTPS. Most, but not all communication with RTM is by HTTPS.

**Licensing:**

GNU GPLv3.0: https://www.gnu.org/licenses/gpl.html.
	
**Version History:**

2/20/2014	v1.00	Initial public release.