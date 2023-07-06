DROP TABLE VC.dbo.Country;

CREATE TABLE VC.dbo.Country(
	CityId int IDENTITY(1,1) PRIMARY KEY,
	City varchar(255),
	DateAdded AS GETDATE()
)

INSERT INTO VC.dbo.Country
VALUES ('Prilep')


INSERT INTO VC.dbo.Country
VALUES ('Skopje')
