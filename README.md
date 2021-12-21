# VRM-Clock
Windows desktop clock widget with embeded VRM system data

![Clock Widget](Screenshot.png)

Display basic information from your Victron system

Lightweight widget, updates data from VRM API once per minute

Show :-

State of Charge, AC in, Generator In and/or PV In

AC Consumption


AC, Generator and PV In only show when active

Edit clock.exe.config (app.config)

		<add key="username" value="your vrm username"/> 	<!-- Put your VRM user name here -->
		<add key="password" value="your vrm password" />  <!-- Put your VRM password here -->
		<add key="soc" value="true" /> <!-- true to display State of Charge otherwise false-->
		<add key="acIn" value="true" /> <!-- true to display AC In otherwise false -->
		<add key="acOut" value="true" /> <!-- true to dislay AC consumption otherwise false -->
		<add key="genset" value="true" /> <!-- true to display Generator AC in otherwise false -->
		<add key="pv" value="true" /> <!-- true to display Solar Production in otherwise false -->
		<add key="language" value="en-US" /> <!-- Language to use for Date display -->
		<add key="colour" value="white" /> <!-- Colour to use for text -->
		<add key="positionX" value="0" /> <!-- X position of widget on screen -->
		<add key="positionY" value="0" /> <!-- Y position of widget on screen -->

Compile from source code or run from \bin\Release\Clock.exe


