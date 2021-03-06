USE [RabbitMQTrace]
GO
/****** Object:  Table [dbo].[RabbitStats]    Script Date: 09/08/2013 16:31:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RabbitStats](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ServerURL] [nvarchar](255) NULL,
	[DateTime] [datetime] NULL,
	[PublishRate] [decimal](9, 2) NULL,
	[DeliverRate] [decimal](9, 2) NULL,
	[AckRate] [decimal](9, 2) NULL,
	[RedeliverRate] [decimal](9, 2) NULL,
	[DeliverGetRate] [decimal](9, 2) NULL,
	[Channels] [int] NULL,
	[Connections] [int] NULL,
	[Consumers] [int] NULL,
	[Exchanges] [int] NULL,
	[Queues] [int] NULL,
 CONSTRAINT [PK_RabbitStats] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[RabbitStats_Samples]    Script Date: 09/08/2013 16:31:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RabbitStats_Samples](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RabbitStatsID] [int] NOT NULL,
	[SampleType] [nvarchar](20) NULL,
	[Timestamp] [bigint] NULL,
	[Sample] [bigint] NULL,
	[Rate] [decimal](9, 2) NULL,
 CONSTRAINT [PK_RabbitStats_Samples] PRIMARY KEY NONCLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
