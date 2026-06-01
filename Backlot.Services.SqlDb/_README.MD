There are 2 ways of using a sql database as data store for your roles.

1) Use a "Dynamic Data Store" principle, which means one (BIG)table containing all roles and one table to manage relations. This is used for applications where you like to add Backlot/Backlot as a way to extend your application.
2) Use a "Static Data Store" principle, which means one table per role. This is used for "legacy" applications where a dataase is already developed.