﻿Imports System.ServiceProcess
Imports System.Text
Public Class BookmakerHorseFeedService
    Inherits System.ServiceProcess.ServiceBase

    Private worker As New Worker()

    Protected Overrides Sub OnStart(ByVal args() As String)

        Dim wt As System.Threading.Thread
        Dim ts As System.Threading.ThreadStart
        gobjEvent.WriteToEventLog("Service Start Banner:    ************************************************")
        gobjEvent.WriteToEventLog("Service Start Banner:    *       BookmakerHorseFeedServiceSTARTING      *")
        gobjEvent.WriteToEventLog("Service Start Banner:    ************************************************")
        gobjEvent.WriteToEventLog("Windows Service OnStart method starting service.")

        ts = AddressOf worker.DoWork
        wt = New System.Threading.Thread(ts)

        wt.Start()

    End Sub

    Protected Overrides Sub OnStop()
        worker.StopWork()
    End Sub

End Class

Public Class Worker

    Private m_thMain As System.Threading.Thread
    Private m_booMustStop As Boolean = False
    Private m_rndGen As New Random(Now.Millisecond)
    Private Shared stdOutput As StringBuilder = Nothing
    Private Shared stdNumOutputLines As Integer = 0
    Private Shared errOutput As StringBuilder = Nothing
    Private Shared errNumOutputLines As Integer = 0
    Private intFileNumber As Integer = FreeFile()
    Public Sub StopWork()

        m_booMustStop = True

        gobjEvent.WriteToEventLog("Service Stopping Banner: ************************************************")
        gobjEvent.WriteToEventLog("Service Stopping Banner: *       BookmakerHorseFeedServiceSTOPPED       *")
        gobjEvent.WriteToEventLog("Service Stopping Banner: ************************************************")

        If Not m_thMain Is Nothing Then

            If Not m_thMain.Join(100) Then

                m_thMain.Abort()

            End If

        End If

    End Sub
    Public Sub DoWork()

        '----------------------------------------------------------'
        'Purpose:   Worker thread.
        '----------------------------------------------------------'

        m_thMain = System.Threading.Thread.CurrentThread

        Dim i As Integer = m_rndGen.Next
        Dim blnReturnStatus As Boolean
        Dim intMins As Integer
        Dim intAdapterCycleEveryMillisecs As Integer = My.Settings.ProcessCycleEverySecs * 1000

        m_thMain.Name = "Thread" & i.ToString
        gobjEvent.WriteToEventLog("Windows worker thread : " + m_thMain.Name + " created.")

        ' Write log entries for configuration settings
        gobjEvent.WriteToEventLog("WorkerThread : Cycle every (secs) : " + My.Settings.ProcessCycleEverySecs.ToString)
        gobjEvent.WriteToEventLog("WorkerThread : Cycle every (millisecs) : " + intAdapterCycleEveryMillisecs.ToString)


        While Not m_booMustStop

            ' Call start process and set status
            blnReturnStatus = StartProcess()

            ' Check status and issue warning
            If blnReturnStatus = False Then
                gobjEvent.WriteToEventLog("WorkerThread : Process returned failed status, service will continue", EventLogEntryType.Warning)
            End If

            '-------------------------------------------------
            '-  Issue heartbeat message every service cycle  -
            '-------------------------------------------------
            If intMins = 0 Then
                gobjEvent.WriteToEventLog("Windows worker thread : Heartbeat.......")
            End If

            '-------------------------------------------------
            '-  Now sleep, you beauty.                       -
            '-------------------------------------------------
            System.Threading.Thread.Sleep(intAdapterCycleEveryMillisecs)

        End While

    End Sub
    Function StartProcess() As Boolean

        ' Define static variables shared by class methods.
        Dim intElapsedTimeMillisecs As Integer = 0

        Try

            ' ------------------------------
            ' Horse Racing 
            ' ------------------------------
            '
            If My.Settings.ProcessBetfred Then

                gobjEvent.WriteToEventLog("StartProcess:    *----------------------------------------")
                gobjEvent.WriteToEventLog("StartProcess:    *---  Betfred - Updating Horse Racing ---")
                gobjEvent.WriteToEventLog("StartProcess:    *----------------------------------------")
                Dim BookmakerHorsesDbClass As New BookmakerHorsesDbClass()
                BookmakerHorsesDbClass.PollBetfredEvents("WIN", "Betfred", My.Settings.BetfredXMLUrl)
                BookmakerHorsesDbClass = Nothing

            End If

            If My.Settings.ProcessWilliamHill Then

                gobjEvent.WriteToEventLog("StartProcess:    *---------------------------------------------")
                gobjEvent.WriteToEventLog("StartProcess:    *---  William Hill - Updating Horse Racing ---")
                gobjEvent.WriteToEventLog("StartProcess:    *---------------------------------------------")
                Dim BookmakerHorsesDbClass As New BookmakerHorsesDbClass()
                BookmakerHorsesDbClass.PollWilliamHillEvents("WIN", "William Hill", My.Settings.WilliamHillXMLUrl)
                BookmakerHorsesDbClass = Nothing

            End If

            ' Match new odds
            Dim BookmakerHorsesDbMatch1 As New BookmakerHorsesDbClass()
            BookmakerHorsesDbMatch1.MatchHorsesWithBookmakers(7, "WIN")
            BookmakerHorsesDbMatch1 = Nothing

        Catch ex As Exception

            gobjEvent.WriteToEventLog("StartProcess : Process has been killed, general error : " & ex.Message, EventLogEntryType.Error)

        End Try

        Return True

    End Function

End Class
