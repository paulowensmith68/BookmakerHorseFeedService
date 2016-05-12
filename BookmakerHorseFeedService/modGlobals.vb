Module modGlobals

    '-------------------------------------------------
    '-   Database connection string                  -
    '-------------------------------------------------
    Public globalConnectionString = My.Settings.ConnectionString

    '-------------------------------------------------
    '-   BetFair Filter                           -
    '-------------------------------------------------
    Public globalBetFairDaysAhead As Integer = My.Settings.DaysAhead
    Public globalStreamSportId As Integer = 7

    '-------------------------------------------------
    '-   Logging objects                             -
    '-------------------------------------------------
    Public gobjEvent As EventLogger = New EventLogger

    '-------------------------------------------------
    '-   Constants                                   -
    '-------------------------------------------------
    Public Const cintShortWaitMillisecs As Integer = 1000
    Public Const cintLongWaitMillisecs As Integer = 5000
    Public Const cintCheckProcessActiveDelayMillisecs As Integer = 5000

    '-------------------------------------------------
    '-   Global                                      -
    '-------------------------------------------------
    Public gintMaximumExecThressholdMillisecs As Integer
    Public gintKillRuntimeThressholdMillisecs As Integer
    Public gblnProcessHasExited As Boolean

End Module
