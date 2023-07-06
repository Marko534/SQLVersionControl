# Control

#USE

Have a directory named Scripts with 
/PreDeployment
/Migrations
/PostDeployment

The scripts in the Pre/PostDeployment always execute.
In migration they are only executed once.

execute the exe with cmd or git bash
path ../Control/bin/Debug/net6.0/Control.exe
  First argument CONECTION STRING (Example: "Data Source=(local)\\SQLEXPRESS;Initial Catalog=VC;Integrated Security=True;Encrypt=False")
  Second argument SCRIPT PATH (Example: "C:\\Users\\*****\\Desktop\\Control\\Control\\Script\\")

