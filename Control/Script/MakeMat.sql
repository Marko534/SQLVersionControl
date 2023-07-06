DROP TABLE VC.dbo.Material;

CREATE TABLE VC.dbo.Material (
	MaterialId int IDENTITY(1,1) PRIMARY KEY,
	MaterialName varchar(255),
	DateAdded as GETDATE()
)

INSERT INTO VC.dbo.Material
VALUES ('wood');

INSERT INTO VC.dbo.Material
VALUES ('iron');

INSERT INTO VC.dbo.Material
VALUES ('coper');

INSERT INTO VC.dbo.Material
VALUES ('gold');