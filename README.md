# The-Penwick-Papers
 Source for The-Penwick-Papers mod for Daggerfall Unity

 ============================ The Penwick Papers ===============================
A collection of features, spells, and items for Daggerfall Unity.

Check Mod Settings to enable/disable various features and options.
Spells and items can't be disabled, but can be ignored.

Many features require the player to be in a specific activation mode (Steal, Grab, Info, Talk).
To reduce tedious mode switching, there are options in the Mod settings to enable mouse buttons 3 and 4
to perform one-shot activation actions.


Enhanced Info
-------------------------------------------------------------------------------
This is a useful feature for all players.
This feature provides more information when activating a creature or NPC while in 'Info' mode.

For enemies, you can see health status, incumbent effects, whether it is weaker/stronger than normal, and disposition.
Instead of 'You see a Grizzly Bear' you might read 'You see a wounded, burning frail Grizzly Bear - Hungry'.

When used on NPCs, you can potentially see their position in a guild, some flavor text, and disposition towards the player character.
Guild status won't be given for the more covert guild types, but the flavor text might give you a clue.
Quest givers will often be described with terms like 'troubled' or 'distracted', making it easier to find quest givers in taverns.
If commoners in the streets are described as such, it means there is trouble in the region (war, famine, crime waves, etc.).
Merchant/Innkeeper descriptions will indicate store/tavern quality.
Disposition information provides the player with some idea of their standing with the NPC's faction.


Grappling hook
-------------------------------------------------------------------------------
This feature requires a grappling hook item be equipped, from Ralzar's Skulduggery mod.
When the player activates a ledge in 'Grab' mode, a climbable grappling hook/rope will be deployed.
It effectively functions as a portable wall.


Landmark Journal
-------------------------------------------------------------------------------
The Landmark Journal allows for fast-travel within the confines of dungeons 
and towns to places the player has already visited and marked. 

It is a fairly common item that can be purchased at most General Stores for a moderate price.

Using the Landmark Journal for travel costs time and fatigue.
These costs are influenced by such factors as:
-Distance travelled
-walking/riding
-Speed attribute
-Encumbrance
-Streetwise skill (if in town)
-Athleticism

While fast-travelling in a dungeon, there is a small possibility of contracting
a disease for characters above level 4.  The probability is influenced by Luck and distance travelled.  
Nearby magic portals in dungeons interfere with navigation.
Nearby enemies prevent navigation.

While fast-travelling in town, there is a possibility of encountering thugs at
night, and cutpurses during the day.  The probability is substantially higher 
if there is an ongoing crime wave in the region.
The Streetwise skill and Acute Hearing can help here.  
If the character's Streetwise and Pickpocket skills are high enough, they can 
simultaneously pick the pockets of any cutpurse encountered.

There is also a possibility of contracting the plague while travelling in town,
if there is an ongoing plague in the region.


Dirty Tricks
------------------------------------------------------------------------------
-Blind
It's the old throw-dirt-in-the-eyes trick, to temporarily blind opponents.
This can be attempted by clicking a facing opponent while in 'Steal' mode, if within range.
This is a contest of Pickpocket(sleight-of-hand) versus Streetwise, with bonuses for agility.
This is a rechargeable ability, with a luck-based recharge time.
See the Blind spell effect for more information.  
Like the Blind spell, most undead are immune. Vampires can be blinded with difficulty.
In addition, some creatures are resistant.
Fair Warning: some of the sneakier opponents can pull the same trick on you...

-Diversion
It's the old throw-pebble-to-distract-opponent trick.
The Pebbles of Skulduggery (from the Skulduggery mod) must be equipped.
To use, click a distant floor or wall while in 'Steal' mode.
Most nearby unengaged opponents will be attracted to the sound and investigate.
The Pebbles of Skulduggery will be depleted, but will be recharged over a time dependent on Luck.

-The Boot
Some find riches and fame in the reaches of The Iliac Bay, others just find a boot to the face.
The Boot is a special attack that sacrifices damage for better knockback.
This can be attempted by clicking an opponent while in 'Grab' mode, if within range.
Success is determined by a contest of Critical Strike versus Dodge, with bonuses for agility.
The amount of knockback is related to character strength and momentum, and opponent weight.
(Note: some enemies, like zombies, are unexpectedly heavy)
The Boot pairs well with its colleague The Ledge.
Attacking enemies are easier to Boot.
Unaware/non-facing enemies are much easier to Boot.
In the future, some enemies may be able to Boot player characters (pending).

-Door Peep
If standing very close to a door, the player can activate the door in 'Info' mode to peep through a small hole.
If crouching, they will peep under the door.

-Door Chock
Inwardly opening doors can be 'chocked', effectively locking them, by a character with sufficient lockpicking skill.
The effective lock level is determined by lockpicking skill.
Chock an unlocked, closed door by clicking on it in 'Steal' mode.  
Remember, inward opening doors only...


Herbalism
------------------------------------------------------------------------------
Herbalism allows the character to use their Medical skill to create herbal remedies.
The remedies include:
 Fatigue/Health/Magicka recovery
 Attribute recovery
 Poison neutralization
 Poison
 A paralysis preventative

There is no herbal remedy for disease; players should seek a nearby Temple of Stendarr, or a temple they are a member of.
 
Herbalism can not be used in combat.
There is significant overlap with the Restoration school of magic.
In general, herbal remedies are slower and usually weaker than the Restoration spells.

Herbalism requires an equipped Mortar&Pestle in addition to other ingredients.
The Mortar&Pestle can often be found for purchase at alchemist stores.
If a character starts with a high enough medical skill, they will begin the game with a Mortar&Pestle and a few ingredients.

Herbal remedies can be applied by crouching and clicking nearby ground or a non-hostile creature while in 'Grab' mode.

The availability and potency of herbal remedies depends on Medical skill.
Typically, a character will learn new remedies whenever they increase Medical skill.
Most remedies only require two ingredients.
There are usually two remedies for the same condition, the second generally has longer duration.  

There are two remedies to neutralize poisons; the remedy to use depends on poison type and will be highlighted.
The remedy typically reduces the poison time, increasing chances for survival. It can sometimes completely neutralize a poison.

Most herbal remedies have some side effect, which is usually drowsiness. In a few cases it is health or magicka damage.

There are three poisons that can be created: Moonseed, Magebane, and Pyrrhic Acid.
The Pyrrhic Acid is the last 'remedy' learned, as it is a strong poison.
When a poison 'remedy' is created, the character enters an Envenomed state.
While in this state, short blades and missile weapons will be continuously coated with poison every round.
Medical skill does not impact poison damage, but does increase the duration of the Envenomed state.
The Envenomed state will end prematurely if the player applies another herbal remedy.

Enemy poison status can be checked using the Enhanced Info feature.

Poisons are useful in long combats, particularly when fighting multiple opponents.
Normal enemy immunities apply.


Trapping
------------------------------------------------------------------------------
The trapping feature allows the player to use their lockpicking (mechanical) skill to create crippling traps using available components.
Available traps fall into the following categories:
  Snares (holds enemies in place for a time)
  Venomous (poison damage)
  Paralyzing
  Flaming Wroth (fire damage)
  The Luna Stick (utility), intended to provide temporary non-flammable lighting when underwater.

To deploy a trap, crouch and click on the ground while in 'Steal' mode.
  
The character's lockpicking skill determines duration of effects, and also the magnitude of damage for venomous and fire traps.
There are multiple versions of most traps, except Flaming Wroth.
Traps are not guaranteed to trigger.  The later trap versions trigger more reliably.
The player character has a much lower chance of triggering their own trap, as they are fully aware of it.
Small creatures (rats) and weightless entities can't trigger traps.

Building and dismantling traps is a convenient way to train lockpicking skill.


Detailed Encumbrance
------------------------------------------------------------------------------
When hovering over the backpack in the inventory screen, a detailed breakdown of item weight will be shown in the panel.
This information will also be shown when clicking the 'Encumbrance' button on the character sheet screen.


Potion Of Seeking
------------------------------------------------------------------------------
Adds a new potion to help with finding objectives in dungeons.
Characters with very low Willpower will receive limited, if any, help.



Governing Attributes
------------------------------------------------------------------------------
This is an experimental implementation of the Governing Attributes described in the Daggerfall game documentation, but never implemented.
If this option is enabled in Mod settings, a skill's advancement rate will be influenced by its governing attribute.
The governing attribute also acts as a 'soft ceiling'; skill advancement beyond the governing attribute value takes twice as long.

The formula used to modify the skill advancement rate is: 1 / Sqrt((governingAttribute - 9) / 50)
...where the governing attribute is between 10-100.  

Here are some sample values:
-Attr-  -Skill Advancement-
 10     roughly 7 times longer
 25     roughly 1.8 times longer
 40     roughly 1.3 times longer
 50     roughly 1.1 times longer
 60     roughly the same as vanilla Daggerfall
 80     roughly 0.84 times longer
 100    roughly 0.74 times longer

Since player characters usually have significantly above average attributes, additional modifications have been made to the skill advancement
rate for various skills to compensate.  For most players, the advancement rate for offensive skills should be similar to the
vanilla Daggerfall experience.  The advancement rate for most non-offensive skills has been accelerated to allow players to achieve reasonable
scores with significantly less forced training and/or practice.

Character leveling effectively unlocks new content.  Depending on skills, your character can potentially level significantly quicker than
normal.  To compensate for that, it is suggested that you increase the skill-per-level rate a bit if you have Governing Attributes enabled.



----------------------Spells----------------------

--Create Atronach
This Mysticism spell allows the caster to construct an atronach minion to aid them.
An ingredient/component must be consumed to cast the spell; the ingredient required depends on the atronach type.
The atronach is a permanent creation, but doesn't leave the dungeon/area when the player exits or fast-travels.
The spell Chance value determines if the atronach is allied with the caster and becomes a minion.
A high Willpower attribute increases the chances of controlling the atronach.
Uncontrolled atronachs can be dangerous, but still useful.
The spell magnitude determines the durability of the atronach.
Speaking to an allied atronach toggles follow/stay behaviour.
An allied atronach can be pushed by activating it in 'Grab' mode.
Atronachs can pick up amulets dropped by the player and equip them, if it is better than what they have.
The maximum number of minions that can follow the player is determined by Willpower.
Following atronachs and undead minions that have been lost will find the player character after a long rest
or when the Landmark Journal fast travel is used.  
There is also a Mod setting to allow for quicker minion teleportation.


--Reanimate
This Mysticism spell allows the caster to reanimate a human corpse to aid them.
A ceremonial dagger (Holy Dagger item) and filled soul gem are required to cast.
Upon casting the spell on a nearby viable vessel, a soul selection window is shown.
The spell Chance value determines if the reanimated creature is allied with the caster.
Like the Create Atronach spell, having a high Willpower helps.
A more powerful soul results in a more durable minion, but is also harder to control.
The type of undead is mostly determined by the vessel the spell is cast on, and soul type in a few cases.
Undead minions can be commanded similarly to atronachs created by the Create Atronach spell.
Undead minions can pick up amulets dropped by the player and equip them, if it is better than what they have.
Skeletal warriors can also equip one-handed weapons and shields.
Liches can equip staves and cloaks.
Equipped weapons won't be used to attack unless the average damage is better than the minion's default.
WARNING: use of this spell has significant negative impact on Divine faction reputations, so temple members should
avoid this spell.  Reputation with a few factions will be increased, however.


--Scour
Converts human corpses into skeletal ones.


--Illusory Decoy
This Illusion spell creates an illusory creature to distract foes.
The form the illusion takes is determined by the caster's best language skills and the current environment.
The decoy will be destroyed if struck, but a high spell magnitude will increase its evasiveness.
A language skill check is made each round to maintain a convincing illusion, with bonuses for Willpower and Personality.
A caster with a high Personality score can create more attractive illusions.


--Blind Spell
This Illusion spell effect is used by the Blinding dirty trick, but can be cast as a spell as well.
Note that most undead are immune to blinding.
Blinded opponents can only see directly in front of them, and must rely on hearing.
The Enhanced Info feature can be used to verify blind status.
If the trickster is stealthy enough, they can often get behind an opponent for easier backstabs.
For best results, wear less armor: avoid heavy armor and foot armor in particular.  
Having a good stealth skill will obviously help as well.


--Wind Walk
This is effectively a flying spell (fast levitation), complete with physics and sound.
It can only be cast outdoors.
The spell magnitude determines acceleration and maximum velocity.
Warning: a high magnitude cast can be potentially dangerous to the caster and others.



 
