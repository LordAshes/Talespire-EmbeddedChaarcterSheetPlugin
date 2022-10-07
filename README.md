# Embedded Character Sheet Plugin

This unofficial TaleSpire plugin for adding an embedded read only character sheet into Talespire from which you
can make rolls. Supports Talespire dice rolling and Chat Roll rolling. Uses concept of layouts to make character
sheet configuration easier but the character data dictates the layout so the GM can mix layouts as needed.

While this plugin is similar in functionality to Character Sheet Plugin there are some major difference and thus
this plugin does not replace the Character Sheet Plugin. Notable difference are:
```
1. Character Sheet Plugin can have multiple sheets open. Embedded Character Sheet can only have one sheet open.
2. Character Sheet Plugin is a form on top of Talespire while Embedded Character Sheet renders inside Talespire.
3. Embedded Character Sheet has better support for controlling the appearance of each sheet element.
```

This plugin, like all others, is free but if you want to donate, use: http://LordAshes.ca/TalespireDonate/Donate.php

## Change Log
```
1.0.0: Initial release
```

## Install

Install using R2ModMan or similar.

## Usage

1. Select a mini for which you want to see the character sheet and for which one is defined.
2. Press the keyboard shortcut for toggling the character sheet or select Character Sheet from the Info radial menu.
3. Depending on the configuration the chartacter sheet will close after making a roll or can be closed using the
toggle character sheet keyboard shortcut.
4. Click on a element associated with a roll to make a roll.

Default Configruation: ``LCTRL`` + ``S``

As always, ``Jon`` is the sample. Name a mini ``Jon`` and then activate the Embedded Character Sheet.

### Roll Method

The roll method can be selected between Talespire dice (use Auto Roll plugin if you want them to be rolled automatically)
or Chat Roll dice. To change this setting, go to the R2ModMan setting for this plugin, find the correspoding setting,
change it and save it. Next time Talespire is run, the roll method will be changed according to the setting.

### Jon Show Option

Once a layout and/or character data is loaded, it is cached to make re-opening the character sheet quicker and not
require disk access. However, in some cases this is not desirable. For example, if the character has leveled up the
character data can be updated outside of Talespire but with the cache, Talespire would not see the changes until the
next time it runs.

To get around this, there is the Jon Snow option. Pressing the corresponding keyboard shortcut causes, like the famous
quote from the series, the plugin to forget all its cached information and thus re-read any layout and/or character data
when it is needed. So, for example, after updating the character sheet data (outside of Talespire), you can use this
option to forget the cached version and thus, when the character sheet it opened, it will load the updated version.

Default Configruation: ``RCTRL`` + ``S``
   
## Configuration

Similar to Character Sheet Plugin the Embedded Character Sheet Plugin uses the concept of Layout and Character Data.
While you can use Layout for the actual entries (and have a Layout for each character sheet) the usual concept is to
make a layout with place holders for the actual data (which is common to all or most minis) and then place the actual
data into a character data file.

Unlike Character Sheet Plugin which used only one layout at a time, Embedded Character Sheet can use many layouts at
the same time so, for example, player minis can use a full character sheet layout while monsters can use a stat block
layout.

In general that means you need 2 or 3 files but then adding additional characters requires only one file. The three
files are:
```
1. Layout file (required - common to multiple minis)
2. Backgound image (optional - common to multiple minis)
3. Character data file (required - per mini)
```

### The Place Holder Concept

The layouts and character data sheets use a concept of place holders. Instead of the layout providing values directly
(which would mean it could not be re-used), the layout defines place holders which are then replaced with actual values
when the layout is used by a specific character. This allows the layout to be used for multiple characters making it
easy to update the layout file without having to edit all of the character files that use it.

There is no specific enforced format for a place holder but place holders should be unique so that they don't show up
in regular text. For example, a place holder of ``wis`` is not a good place holder because if the layout wants to write
out the world ``wisdom`` the first portion of it would match the place holder and thus get replaced. The typical convention
is to use brace brackets around place holders. If the place holder is ``{wis}`` then it will not be found in the word
``wisdom`` and thus such a word will be possible to display.

The place holders can be nested to make calculations easier. For example a place holder of ``{Attack1}`` could resolve
to 1D20+{BAB}+{STR} which then resolves to 1D20+7+4.

### Layout File

The layout file defines where things are displayed on the character sheet using a JSON file. The name of the layout
file needs to be as follows: ``EmbeddedCharacterSheet.Layout.Name.csl`` where _Name_ can be any unique identification
for that particular layout. For example: ``EmbeddedCharacterSheet.Layout.StatBlock.csl``

The contents are as follows:
```
{
  "background": "Sample_3.5E.png",
  "position": {
    "x": 10,
    "y": 40
  },
  "size": {
    "w": 293,
    "h": 512
  },
  "elements": [
    {
      "type": 0,
      "position": {
        "x": 52,
        "y": 88
      },
      "size": {
        "w": 230,
        "h": 10
      },
      "style": "fontSize=12|fontStyle=1|normal.textColor=220,220,200",
      "content": "{CharName}",
      "roll": ""
    },
    {
      "type": 0,
      "position": {
        "x": 52,
        "y": 116
      },
      "size": {
        "w": 70,
        "h": 10
      },
      "style": "fontSize=10|fontStyle=2|normal.textColor=128,64,32",
      "content": "{CharRace}",
      "roll": ""
    }
  ]
}
```
Where _background_ is the name of the background image. Empty string (default) is no background image is desired.
Where _position_ is the top left corner of the character sheet.
Where _size_ is the width and height of the character sheet.
Where _elements_ is an array of GuiElements that define all of the visual elements of the character sheet (besides the background).

A GuiElement has the following properties...

The _type_ determines the type of element (0=label, 1=button). Currently both seem to render the same, so this can be ignored for now.
The _position_ determines the position of the element on the character sheet (with respect to the character sheet not screen).
The _size_ determines the width and height of the element on the character sheet.
The _content_ determines what is displayed. If the content is surrounded by ``#`` characters, it is treated as a texture file name and
displays the corresponding image. Otherwise it is treated as text with all placed holders replaced.
The _roll_ determines what is rolled when the element is clicked. If it is an empty string (default) then nothing happens when the
element is clicked. Otherwise the content, after replacing all place holders, is sent to the corresponding rolling method. It is
important that the roll resolve to something in the form ``#D#+#`` or ``#D#-#``. This is especially true when using the Talespire dice
rolling method. The Chat Roll method may support other formats in addition to the ones listed but sticking to the formats indicated
means the roll will be compatible with both rolling methods. The roll parameter does support multiple rolls by using a slash to
separate rolls. For example: ``1D20+7/2D6+3``
The _style_ determins the apperance of the element and is used to override the (configurable) defaults. It is a pipe delimited string
of GuiStyle proeprties (see Unity documentation for all GuiStyle properties). The most common use of this is to change the color, size
and style of the element text. When using this method to change color, three formats are accepted: color name, RGB and ARGB. For color
name just eneter the color name like "red". For RBG and ARGB list each of the components (as a byte) separated by commas.

Note: Rolls can resolve to a #D# specification with any number of numeric bonuses and/or penalties. The total modifier is calculated
before passing the roll to the rolling method. For example, ``1D20+3+2-5`` is a valid roll specification.
Note: The above shows 2 GuiElements but the actual layout will have many more elements.

### Character Data

The character data file defines the values of the various place holders in the layout file. This allows one layout file to be used for
multiple characters without needing to copy the whole layout contents into each character sheet. This means that when an update is
needed, the layout file can be updated and the change will be automatically applied to all characters that use the layout instead of
having to update each character sheet for each character. The filename of the character data need to be as follows:
``EmbeddedCharacterSheet.Data.MiniName.csd`` where _MiniName_ is the name assigned to the mini whose character sheet is being looked up.
For example: ``EmbeddedCharacterSheet.Data.Jon.csd``

The contents of the character data file is as follows:
```
{
  "layout": "5E",
  "stats": {
	"{CharName}": "James Bond",
	"{CharRace}": "Human",
	"{CharClass}": "Spy",
    "{STR}": "2",
    "{DEX}": "4",
    "{CON}": "2",
    "{INT}": "3",
    "{WIS}": "2",
    "{CHA}": "4",
  }
}
```

Where _layout_ indicates the layout that the data is plugged into. This property is required.
Where _stats_ is a list of place holder names and their values.

It is important to understand that layout file and character data files are completely game independent. That means that the stat values
in the character data file is comepletely up to the user. The only stipulation is that the character sheet file must contain a a value for
each place holder used in the referenced layout. So if the layout has a place holder for {PicklesPerDay} the character data file that uses
that layout needs to define that place holder value even if just as a empty string. As such the above stats are an _example_ which suggest
a D&D style game but this plugin can equally be used for any dice rolling game system.

### Character Vs Group Data

Most of the documentation refers to a character sheet for a mini or character implying a one to one relationship. However, this is not
necessarily true. As implied above by the file naming requirement, the plugin connects the selected mini to a character sheet by the mini
name. As such, you can have a group of enemies, for exmample, goblins use the same character sheet by naming them all the same name,
such as goblin. Then you can make a character sheet for goblin and no matter which goblin you select the same character sheet will be
displayed. If you need to distinguish between them (and you can't by name because of this plugin), you can use GM Notes to place text
above the minis.
