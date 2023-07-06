DROP TABLE VC.dbo.City;

CREATE TABLE VC.dbo.City(
	CityId int IDENTITY(1,1) PRIMARY KEY,
	City varchar(255),
	DateAdded AS GETDATE()
)

INSERT INTO VC.dbo.City
VALUES ('Prilep')


INSERT INTO VC.dbo.City
VALUES ('Skopje')
