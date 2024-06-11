# AT_Commander_18029633
### Description

In this project the goal was to create an AI assistant to help with micro-management for players in RTS-style video games. This project uses a free RTS Toolkit, which offered a framework using deferred update cycles and partial update cycles as it was seeking to allow for the simulation of hundreds or even thousands of units. By working within this framework it was easy to simulate a responsiveness slider for the AI commander, which in a fully developed game could be a value on the commander that could develop over time. The RTS toolkit also allowed for easily mirroring systems to create an RTS style ranged combat without projectiles. The AI offers unit suggestions too based on its current pool of units, and it's capable of calculating a balance of power and adjusting to unit counters if unit stats change or additional units would be added to the game. It uses damage, armour penetration, rate of fire, armour and health in its calculations, but not range or reload time as of yet. In addition there is a supply system, which the AI also manages, choosing the best units for supplying and enough to make sure all units stay supplied.

### Instructions

Two scenes are available, where different settings can be checked out in the 'Assets/Scenes/' folder named 'Basic' and 'Counters'. The 'AI_Control' has the 'AI_Controller' script, which contains all the modifiable variables for the AI to use. By default these were configured to be Q to add units to the AI's control, C to remove them, Z to start planning and pathing, and X to execute the attack. There are supply buildings and unit buildings, unit buildings allow for the training of units via the 1, 2, 3 and 4 keys once they are selected.

### Source Code

Unity Project, custom additions are integrated to the Free RTS toolkit, which include supply system, ranged combat, armour penetration for ranged, AI and AI controls.

### Learning Outcomes

Learning about deferred and partial update cycles, RTS gameplay design and theory.

### Links
[RTS Toolkit](https://assetstore.unity.com/packages/templates/systems/rts-toolkit-free-30247).

### Credits
[Romas Smilgys](https://www.linkedin.com/in/romas-smilgys-a4134984/) for the free RTS Toolkit.
