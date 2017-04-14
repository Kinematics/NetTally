# NetTally
The purpose of this program is to allow automated vote tallying for forum quests.

Forum quests generally have a single Gamemaster/Questmaster (GM/QM), typically the thread OP, who writes part of a story, then provides options of what to do after each update.  The other forum users then have the option to choose one or more of the provided options, or write in a custom plan.

In order to resolve which plan to actually follow in the next update post, a method of counting the votes is needed. Thus, this program (or others like it).

Full documentation on the use of this program (including vote-formatting guidelines) is available in the wiki: https://github.com/Kinematics/NetTally/wiki


### Requirements

#### Current active branch: VS2017 / dev

System requirements: .NET 4.6.

This will not run on Windows XP, since the highest version of .NET available is 4.0.

The core library targets .NET Standard 1.3 (equivalent to .NET 4.6).  The Windows UI targets .NET 4.6.  The console app targets .NET Core 1.1.

If you wish to edit the code, it requires Visual Studio 2017, and uses C#7.

#### Retired branch: VS2015

System requirements: .NET 4.5.

This will not run on Windows XP, since the highest version of .NET available is 4.0.

If you wish to edit the code, it requires Visual Studio 2015, and uses C#6.


### Quickstart overview

Add a new quest by pasting in the URL of the thread that's being followed.

Votes in the quest are marked with [x] or [X] in a user's post.  Nesting a line under another can be done by prefixing that with a hyphen (-).

Example:
```
[x] Fire the gun
-[x] At the target on the left
```

Proxy votes allow you to specify another user or plan name whose vote you wish to support.

Example:
```
[x] Username1
```

Partitioning votes breaks individual votes into single lines or blocks.  Partitioning by line treats every single line of each vote independently, while partitioning by block treats subvotes as part of each main vote entry.

BBCode formatting is preserved, including italics, bold, underlines, colors, and URLs, as long as it's part of the vote content (but not if it covers the entire line, or part of the marker area or whatever).  Quoted votes are ignored.

The program is currently capable of understanding XenForo and vBulletin forum systems.  Other forums have the framework code in place, but the forum software often has limitations that prevent NetTally from being able to properly operate on them. (EG: Lack of post numbers, lack of page numbers, etc.)

This is built on the basis of quests run on SpaceBattles and SufficientVelocity, and is an adaptation of a program originally written by Firnagzen.


