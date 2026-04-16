National Park Tracker Application

This will be a .NET 10 Blazor Server application.  It will be used to track which United States National Parks the user has been to and which ones they have not.

Use Postgres as the database, I have that installed locally.

The UI should allow the user to sign in.  It should display a map that show pins for all of the National Parks in the Parks table.  The parks that the user has visited should show with a red pin.  The parks that the user has not visited yet, should show a clear pin.

When the user clicks on a pin that they have visited, it should show the park information and that date(s) that they visited the park.

There should be a button that allows the user to add a new visit.  When they click the button, a pop up should display, allowing them to select the park, by selecting from a list of park names.  They should also be able to specify the date when they visited, although that field should not be required.  When they hit the save button, a new record should be saved to the visits table.

There should also be an Admin screen that allows the user to see all of the parks that are currently in the table, and allow the user to enter a new park.
