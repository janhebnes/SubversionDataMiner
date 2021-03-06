USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[SvnRepositoryRevision]    Script Date: 09-12-2015 16:52:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SvnRepositoryRevision](
	[SvnRepository] [nvarchar](200) NOT NULL,
	[Revision] [int] NOT NULL,
	[Author] [nvarchar](200) NULL,
	[Time] [datetime] NULL,
	[LogMessage] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
