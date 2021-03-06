USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[SvnRepositoryRevisionChangedPaths]    Script Date: 09-12-2015 16:52:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SvnRepositoryRevisionChangedPaths](
	[SvnRepository] [nchar](200) NOT NULL,
	[Revision] [int] NOT NULL,
	[Action] [nvarchar](100) NULL,
	[Path] [nvarchar](300) NULL,
	[CopyFromRevision] [int] NULL,
	[CopyFromPath] [nvarchar](300) NULL
) ON [PRIMARY]

GO
