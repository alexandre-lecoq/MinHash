SET NOCOUNT ON ;
GO

-- !! MODIFY TO SUIT YOUR TEST ENVIRONMENT !!
USE MinhashTest
GO

-------------------------------------------------------------------------------------------------------------------------------

IF EXISTS ( SELECT  *
            FROM    sys.objects
            WHERE   object_id = OBJECT_ID(N'[dbo].[JACCARD_INDEX_64]')
                    AND type = N'FS' ) 
    DROP FUNCTION [dbo].[JACCARD_INDEX_64]
GO

IF EXISTS ( SELECT  *
            FROM    sys.objects
            WHERE   object_id = OBJECT_ID(N'[dbo].[JACCARD_INDEX_8]')
                    AND type = N'FS' ) 
    DROP FUNCTION [dbo].[JACCARD_INDEX_8]
GO

IF EXISTS ( SELECT  *
            FROM    sys.assemblies asms
            WHERE   asms.name = N'Minhash'
                    AND is_user_defined = 1 ) 
    DROP ASSEMBLY [Minhash]
GO
