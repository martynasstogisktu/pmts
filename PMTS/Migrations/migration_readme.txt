During the setup of the database the initial migration should be performed using the following commands:

Add-Migration InitialMigration
Update-Database

If there are subsequent changes to the code which require the database to be updated (for example if a new variable is added to one of the Model classes), another migration would neet to be performed:

Add-Migration [MigrationName]
Update-Database
