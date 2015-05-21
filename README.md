# NetTally
The purpose of this program is to allow automated vote tallying for forum quests.

System requirements: .NET 4.5.2.  This will not run on Windows XP.

Build requirements: Visual Studio 2015

Forum quests generally have a single Gamemaster/Questmaster (GM/QM), typically the thread OP, who writes part of a story, then provides options of where to go from there.  The other forum users then have the option to choose one or more of the provided options, or write in a custom plan.

In order to resolve which plan to actually follow in the next GM post, a method of counting up the votes is needed. Thus, this program.

Add a new quest by pasting in the URL of the thread that's being followed.  The page number and any anchor text at the end of the URL will be stripped off, but otherwise you can edit it to fit actual usage needs.

Once a quest has been entered, you may hit F2 (or the Edit Name button) to edit the displayed name (what's shown in the combobox dropdown), and F2 again to edit the original thread URL, if needed.  Hitting Escape will cancel any edit.

Votes are identified by a bracketed x (or X), +, or checkmark at the start of each line of the vote.  So a vote line may start in one of the following ways: `[x] [X] [+] [✓] [✔]`

Sub-votes use the hyphen character (-) to indent additional lines.  This allows you to refine the detail of a primary vote line.  Subvote lines are included with the main vote line when partitioning by block.

Example:
```
[x] Fire the gun
-[x] At the target on the left
```

Referral votes allow you to specify another user name whose vote you wish to support.

Example:
```
[x] Username1
```

Partitioning votes breaks individual votes into single lines or blocks.  Partitioning by line treats every single line of each vote independently, while partitioning by block treats subvotes as part of each main vote entry.

BBCode formatting is preserved, including italics, bold, underlines, and colors.  Quoted votes are ignored.

The program is currently capable of parsing XenForo and vBulletin forum systems.  The number of tested forums is low, however, so refinements will be necessary to make it more broadly available.

This is built on the basis of quests run on SpaceBattles and SufficientVelocity, and is an adaptation of a program originally written by Firnagzen.

