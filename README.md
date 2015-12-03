# NetTally
The purpose of this program is to allow automated vote tallying for forum quests.

Forum quests generally have a single Gamemaster/Questmaster (GM/QM), typically the thread OP, who writes part of a story, then provides options of what to do after each update.  The other forum users then have the option to choose one or more of the provided options, or write in a custom plan.

In order to resolve which plan to actually follow in the next GM post, a method of counting up the votes is needed. Thus, this program (and other similar ones).

Full documentation on the use of this program (including vote-formatting guidelines) is available in the wiki: https://github.com/Kinematics/NetTally/wiki


###Requirements

System requirements: .NET 4.5.2.

This will not run on Windows XP, as .NET 4.5 is only available for Vista or higher.

An Android version is not available, because of the cost of the software needed to compile for that platform.

If you wish to edit the code, it requires Visual Studio 2015 for C# 6.


###Quickstart overview

Add a new quest by pasting in the URL of the thread that's being followed.

Votes in the quest are marked with [x] or [X] in a user's post.  Nesting a line under another can be done by prefixing that with a dash (-).

Example:
```
[x] Fire the gun
-[x] At the target on the left
```

Referral votes allow you to specify another user or plan name whose vote you wish to support.

Example:
```
[x] Username1
```

Partitioning votes breaks individual votes into single lines or blocks.  Partitioning by line treats every single line of each vote independently, while partitioning by block treats subvotes as part of each main vote entry.

BBCode formatting is preserved, including italics, bold, underlines, and colors, as long as it's part of the vote content (but not if it covers the entire line, or part of the marker area or whatever).  Quoted votes are ignored.

The program is currently capable of parsing XenForo and vBulletin forum systems.  The number of tested forums is low, however, so refinements will be necessary to make it more broadly available.

This is built on the basis of quests run on SpaceBattles and SufficientVelocity, and is an adaptation of a program originally written by Firnagzen.


