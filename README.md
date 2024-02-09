# ShowEscalationValue

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) that shows the escalation and war value of overworld sites and patrols.

These fixes are for release version **1.2.1**.

The overworld story is driven by liberating provinces. When you first enter an occupied province, you start out in a raiding mode where each action you take may increase an escalation value. This is a rough proxy of how aware the enemy is of your presence in that province. When you emerge victorious from combat with a patrol or a garrison at a site, you will increase the escalation value. However, you don't know how much that value will increase before engaging in combat. This mod will show the escalation value of each occupied site and enemy patrol, both on the map next to the site or patrol and also in the information card that pops up in the bottom right corner when you hover over a site/patrol or click on one.

After the escalation value in a province crosses a threshold, the province may become contested. A new metric, the war score, will appear. You and the enemy will have a separate war score. Every combat now changes both the escalation value and the war score. This mod will show the change to the enemy's war score if you beat a patrol or garrison at a site. It appears as a third value near the site or patrol on the map and replaces the escalation value shown in the information card as the war score is more important than the escalation value in a contested province.

Here's an example screenshot showing two sites and two patrols in a province being raided and a site in a contested province. In the middle of the screen is a site in the raiding province that I've hovered over to bring up the information card in the bottom right corner of the screen. The numbers next to the site icon on the map show the threat rating followed by a slash and then the increase in escalation value if you attack the site and win the combat. You'll see at the top left of the information card the escalation value of the site shown as "EV 32". In the upper right quadrant, above the information card, there's a site in a contested province. This site has three numbers next to it, the same threat rating slash escalation value increase as with the site in the raiding province, followed by a slash and a new number which is the change (decrease) in the enemy's war score if you take the site. The escalation value is shown slightly faded since it's not the primary score used in a contested province.

![Four sites in a raiding province with threat rating/escalation value and a site in a contested province with threat rating/escalation value/war score change](https://github.com/echkode/PhantomBrigadeMod_ShowEscalationValue/assets/48565771/51bf7acf-c4d4-46ec-afc1-3304cf032d89)

The escalation value increase or war score decrease is also shown in the information card that appears in the bottom right of the screen in the combat event screen. This is the screen that's first shown when you come within the combat range of a site or patrol and it asks if you want to enter combat or disengage. In a province that you're just raiding, the escalation value increase is shown.

![Escalation value increase shown on the information card when in the combat event screen](https://github.com/echkode/PhantomBrigadeMod_ShowEscalationValue/assets/48565771/5d9c6f2e-719b-4a55-a86e-49ea197e8589)

For a contested province, the war score decreased is shown as "WS 12".

![War score decrease shown on the information card when in the combat event screen in a contested province](https://github.com/echkode/PhantomBrigadeMod_ShowEscalationValue/assets/48565771/1afbbb87-b592-4451-a47b-632a46f15bea)

This mod also fixes a small bug where the information card in the event screen was sometimes shifted down too much. Here's a screenshot of that bug.

![Information card shifted down too much in event screen, clipping the garrison information](https://github.com/echkode/PhantomBrigadeMod_ShowEscalationValue/assets/48565771/5feefe52-b991-4a8a-a3cf-0ef3254d5e5b)
