-- Installation script for JACCARD_INDEX_8 functions.
SET NOCOUNT ON ;
GO

-- !! MODIFY TO SUIT YOUR TEST ENVIRONMENT !!
USE MinhashTest
GO

-------------------------------------------------------------------------------------------------------------------------------

-- Turn advanced options on
EXEC sys.sp_configure @configname = 'show advanced options', @configvalue = 1 ;
GO
RECONFIGURE WITH OVERRIDE ;
GO

-- Enable CLR
EXEC sys.sp_configure @configname = 'clr enabled', @configvalue = 1 ;
GO
RECONFIGURE WITH OVERRIDE ;
GO

-- Enable CLR
--EXEC sys.sp_configure @configname = 'clr strict security', @configvalue = 0 ;
GO
RECONFIGURE WITH OVERRIDE ;
GO

-------------------------------------------------------------------------------------------------------------------------------

SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, QUOTED_IDENTIFIER ON;
SET CONCAT_NULL_YIELDS_NULL, NUMERIC_ROUNDABORT OFF;
GO
IF EXISTS (SELECT * FROM tempdb..sysobjects WHERE id=OBJECT_ID('tempdb..#tmpErrors')) DROP TABLE #tmpErrors
GO
CREATE TABLE #tmpErrors (Error int)
GO
SET XACT_ABORT ON
GO
SET TRANSACTION ISOLATION LEVEL READ COMMITTED
GO
BEGIN TRANSACTION
GO

-------------------------------------------------------------------------------------------------------------------

PRINT N'Creating [Minhash]...';
GO
CREATE ASSEMBLY [Minhash]
    AUTHORIZATION [dbo]
	FROM _MINHASH_SQLSERVERCLR_DLL_HEX_
    WITH PERMISSION_SET = SAFE;
GO
IF @@ERROR <> 0
   AND @@TRANCOUNT > 0
    BEGIN
        ROLLBACK;
    END
IF @@TRANCOUNT = 0
    BEGIN
        INSERT  INTO #tmpErrors (Error)
        VALUES                 (1);
        BEGIN TRANSACTION;
    END
GO
EXEC sys.sp_addextendedproperty 
    @name = N'URL',
    @value = N'https://github.com/alexandre-lecoq/MinHash',
    @level0type = N'ASSEMBLY',
    @level0name = N'Minhash'
GO

-------------------------------------------------------------------------------------------------------------------

PRINT N'Creating [dbo].[JACCARD_INDEX_8]...';
GO
CREATE FUNCTION [dbo].[JACCARD_INDEX_8](@leftArray VARBINARY(8000), @rightArray VARBINARY(8000))
    RETURNS FLOAT
    EXTERNAL NAME [Minhash].[MinHash.SqlServerClr.Minhash].[JaccardIndex8];
GO
IF @@ERROR <> 0
   AND @@TRANCOUNT > 0
    BEGIN
        ROLLBACK;
    END
IF @@TRANCOUNT = 0
    BEGIN
        INSERT  INTO #tmpErrors (Error)
        VALUES                 (1);
        BEGIN TRANSACTION;
    END
GO

-------------------------------------------------------------------------------------------------------------------

PRINT N'Creating [dbo].[JACCARD_INDEX_64]...';
GO
CREATE FUNCTION [dbo].[JACCARD_INDEX_64](@leftArray VARBINARY(8000), @rightArray VARBINARY(8000))
    RETURNS FLOAT
    EXTERNAL NAME [Minhash].[MinHash.SqlServerClr.Minhash].[JaccardIndex64];
GO
IF @@ERROR <> 0
   AND @@TRANCOUNT > 0
    BEGIN
        ROLLBACK;
    END
IF @@TRANCOUNT = 0
    BEGIN
        INSERT  INTO #tmpErrors (Error)
        VALUES                 (1);
        BEGIN TRANSACTION;
    END
GO

-------------------------------------------------------------------------------------------------------------------

IF EXISTS (SELECT * FROM #tmpErrors) ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT>0 BEGIN
PRINT N'The transacted portion of the database update succeeded.'
COMMIT TRANSACTION
END
ELSE PRINT N'The transacted portion of the database update failed.'
GO
DROP TABLE #tmpErrors

-------------------------------------------------------------------------------------------------------------------
GO
