CREATE TABLE VC.dbo.TransportMeathod(
	TransportMeathodId int IDENTITY(1,1) PRIMARY KEY,
	TransportMeathod varchar(255),
	DateAdded AS GETDATE()
);

INSERT INTO VC.dbo.TransportMeathod
VALUES ('Boat');


INSERT INTO VC.dbo.TransportMeathod
VALUES ('Train')


INSERT INTO VC.dbo.TransportMeathod
VALUES ('Truck')
