Release notes 2015-06-25 "Early Access" (Because all the cool kids do it)

Index:
======
1. Purpose of The Software
2. Requirements
3. "Setting up" or "Help!"
4. Supported HP Printer Models
5. Changelog


1. Purpose of The Software
==========================
This tool reads a CSV file and queries the printers in said file to get the following information:
	- Hostname (DEP)
	- Serialnumber
	- Pagecounter for monochromatic prints
	- Pagecounter for colored prints
	- bool if the printer is currently connected
It then queries the printer and reads the pagecounters from the HP usage page, calculates differences and writes these infos in a .csv file.

This program works in the command line of Microsoft Windows and is coded in C#.


2. Requirements
===============
The obvious ones:
	- HP printers
	- .Net 4.5 (recommended. older versions should work)

The "not-so-obvious" ones:
	- a secure network. The certificates of the printers are selfsigned, so this program accepts ANY certificate or otherwise you'd need to fill the database with hashes of each certificate. Thank me later.

	
3. "Setting up" or "Help!"
==========================
Right now? Uhm... Just add printers as a list element. This is somekind of an "Early Access" release, so - what do you expect? 


4. Supported HP Printers
========================
Note: Other printers MIGHT work with this. This list has been tested by me or others. Want a specific model? Open an Issue with the model and send me the HTML code via gist. Or code it and add it, maybe even share it with me. That'd be nice.
	- Office Jet
		- Color FlowMFP X585
	- LaserJet
		- 600 M602
