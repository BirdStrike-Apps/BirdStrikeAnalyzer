Name: Owen Rogers

Implemented Features:
    I generated the bird strikes reports page with a filter button 
that brings in a pop up with values the user can use to filter
the reports. It will be where the user can view every submitted
report sent from the app.

How I Supervised the AI:
    I gave it a PDF file of my figma project and asked it to create 
it with xaml and xaml.cs files. It did a good job replicating the 
figma project. The hamburger menu button weirdly turned off and on 
the side panel. It was unneeded because the project already has a 
hamburger menu button so I completely removed it. The filter button 
worked as intended so I didn't need to do anything there.

Problems:
    I had difficulty with getting InitializeComponent to work. 
As a matter of fact, a lot of code was being flagged as an error 
because it did exist in context. It fixed itself when I ran the 
code on the emulator. 