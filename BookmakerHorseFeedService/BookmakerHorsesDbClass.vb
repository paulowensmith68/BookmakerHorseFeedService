Imports System.Xml
Imports MySql.Data.MySqlClient

Public Class BookmakerHorsesDbClass
    ' Holds the connection string to the database used.
    Public connectionString As String = globalConnectionString
    Public eventList As New List(Of BookmakerHorsesEventClass)
    Public matchedList As New List(Of MatchedEventClass)

    'Holds message received back from class
    Public returnMessage As String = ""

    'Vars used for output message
    Private insertCount As Integer = 0
    Private updateCount As Integer = 0

    Public Sub PollBetfredEvents(marketTypeCode As String, bookmakerName As String, bookmakerURL As String)

        Dim newEvent As BookmakerHorsesEventClass

        Try

            ' Processing event...
            gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Processing events from Betfred XML feed", EventLogEntryType.Information)

            Dim reader As XmlTextReader = New XmlTextReader(bookmakerURL)

            Dim strBookMakerName As String = bookmakerName
            Dim strMeeting As String = ""
            Dim strRace As String = ""
            Dim strEventDate As String = ""
            Dim strEventTime As String = ""
            Dim strMarketTypeCode As String = marketTypeCode
            Dim strBetName As String = ""
            Dim strBetNameShort As String = ""
            Dim dblPrice As Double

            Dim blnFoundEvent As Boolean = False
            Dim blnFoundBet As Boolean = False

            ' Loop through each event
            Do While (reader.Read())
                Select Case reader.NodeType
                    Case XmlNodeType.Element 'Display beginning of element.

                        ' Case for each element
                        Select Case reader.Name
                            Case "event"

                                ' Reset flags
                                blnFoundEvent = True
                                blnFoundBet = False

                                ' 
                                strMeeting = ""
                                strRace = ""
                                strEventDate = ""
                                strEventTime = ""
                                strBetName = ""
                                strBetNameShort = ""
                                dblPrice = 0

                                ' Step through attributes
                                If reader.HasAttributes Then 'If attributes exist
                                    While reader.MoveToNextAttribute()
                                        Select Case reader.Name
                                            Case "name"
                                                strRace = reader.Value
                                            Case "date"
                                                strEventDate = reader.Value
                                            Case "time"
                                                strEventTime = reader.Value
                                            Case "meeting"
                                                strMeeting = reader.Value
                                        End Select
                                    End While
                                End If

                            Case "bet"
                                ' Step through attributes
                                If reader.HasAttributes Then 'If attributes exist
                                    While reader.MoveToNextAttribute()
                                        Select Case reader.Name
                                            Case "name"
                                                strBetName = reader.Value
                                            Case "short-name"
                                                strBetNameShort = reader.Value
                                            Case "priceDecimal"
                                                Dim succeed As Boolean
                                                succeed = Double.TryParse(reader.Value, dblPrice)
                                                If succeed Then
                                                    blnFoundBet = True
                                                Else
                                                    blnFoundBet = False
                                                End If
                                        End Select
                                    End While
                                End If

                        End Select

                    Case XmlNodeType.Text 'Display the text in each element.
                    Case XmlNodeType.EndElement 'Display end of element.

                End Select

                ' Found full set
                If blnFoundEvent And blnFoundBet Then

                    ' Convert date/time to timestamp
                    Dim iString As String = strEventDate.Substring(0, 4) + "-" + strEventDate.Substring(4, 2) + "-" + strEventDate.Substring(6, 2) + " " + strEventTime.Substring(0, 2) + ":" + strEventTime.Substring(2, 2)
                    Dim tsEventTimestamp As DateTime = DateTime.ParseExact(iString, "yyyy-MM-dd HH:mm", Nothing)

                    If CheckValidCourse(strMeeting) Then

                        newEvent = New BookmakerHorsesEventClass With {
                         .bookmakerName = strBookMakerName,
                         .meeting = strMeeting,
                         .race = strRace,
                         .eventTimestamp = tsEventTimestamp,
                         .marketTypeCode = strMarketTypeCode,
                         .betName = strBetName,
                         .price = dblPrice
                        }

                        ' Add to list
                        eventList.Add(newEvent)

                    End If

                    ' Reset flags
                    blnFoundBet = False

                End If
            Loop

        Catch ex As Exception
            gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Error reading XML data : " + ex.Message, EventLogEntryType.Error)
            Exit Sub

        Finally

        End Try

        ' Log numbers from XML
        gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Extracted " + eventList.Count.ToString + " horses from Betfred XML feed", EventLogEntryType.Information)

        ' Write to database
        Dim strResult As String
        gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Starting database update . . . . ", EventLogEntryType.Information)
        strResult = WriteEventList(bookmakerName, marketTypeCode, True)
        gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Response from Database Update: : " + strResult, EventLogEntryType.Information)

    End Sub

    Public Sub PollWilliamHillEvents(marketTypeCode As String, bookmakerName As String, bookmakerURL As String)

        Dim newEvent As BookmakerHorsesEventClass

        Try

            ' Processing event...
            gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Processing events from William Hill XML feed", EventLogEntryType.Information)

            Dim reader As XmlTextReader = New XmlTextReader(bookmakerURL)

            Dim strBookMakerName As String = bookmakerName
            Dim strMeeting As String = ""
            Dim strRace As String = ""
            Dim strEventDate As String = ""
            Dim strEventTime As String = ""
            Dim strMarketTypeCode As String = marketTypeCode
            Dim strBetName As String = ""
            Dim strBetNameShort As String = ""
            Dim dblPrice As Double

            Dim blnFoundEvent As Boolean = False
            Dim blnFoundBet As Boolean = False

            ' Loop through each event
            Do While (reader.Read())
                Select Case reader.NodeType
                    Case XmlNodeType.Element 'Display beginning of element.

                        ' Case for each element
                        Select Case reader.Name
                            Case "market"

                                ' Reset flags
                                blnFoundEvent = False
                                blnFoundBet = False

                                ' Empty
                                strMeeting = ""
                                strRace = ""
                                strEventDate = ""
                                strEventTime = ""
                                strBetName = ""
                                strBetNameShort = ""
                                dblPrice = 0

                                ' Step through attributes
                                If reader.HasAttributes Then 'If attributes exist
                                    While reader.MoveToNextAttribute()
                                        Select Case reader.Name
                                            Case "name"
                                                If reader.Value.EndsWith("- Win") Then

                                                    ' Found event
                                                    blnFoundEvent = True

                                                    Dim s As String = reader.Value
                                                    Dim words() As String = s.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
                                                    strRace = words(1) + " " + words(0)
                                                    strMeeting = words(1)

                                                End If
                                            Case "date"
                                                strEventDate = reader.Value
                                            Case "time"
                                                strEventTime = reader.Value
                                        End Select
                                    End While
                                End If

                            Case "participant"
                                ' Step through attributes
                                If reader.HasAttributes Then 'If attributes exist
                                    While reader.MoveToNextAttribute()
                                        Select Case reader.Name
                                            Case "name"
                                                strBetName = reader.Value
                                            Case "oddsDecimal"
                                                Dim succeed As Boolean
                                                succeed = Double.TryParse(reader.Value, dblPrice)
                                                If succeed Then
                                                    blnFoundBet = True
                                                Else
                                                    blnFoundBet = False
                                                End If
                                        End Select
                                    End While
                                End If

                        End Select

                    Case XmlNodeType.Text 'Display the text in each element.
                    Case XmlNodeType.EndElement 'Display end of element.

                End Select

                ' Found full set
                If blnFoundEvent And blnFoundBet Then

                    ' Convert date/time to timestamp
                    Dim iString As String = strEventDate + " " + strEventTime.Substring(0, 2) + ":" + strEventTime.Substring(3, 2)
                    Dim tsEventTimestamp As DateTime = DateTime.ParseExact(iString, "yyyy-MM-dd HH:mm", Nothing)

                    If CheckValidCourse(strMeeting) Then

                        newEvent = New BookmakerHorsesEventClass With {
                             .bookmakerName = strBookMakerName,
                             .meeting = strMeeting,
                             .race = strRace,
                             .eventTimestamp = tsEventTimestamp,
                             .marketTypeCode = strMarketTypeCode,
                             .betName = strBetName,
                             .price = dblPrice
                            }

                        ' Add to list
                        eventList.Add(newEvent)

                    End If

                    ' Reset flags
                    blnFoundBet = False

                End If

            Loop

        Catch ex As Exception
            gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Error reading XML data : " + ex.Message, EventLogEntryType.Error)
            Exit Sub

        Finally

        End Try

        ' Log numbers from XML
        gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Extracted " + eventList.Count.ToString + " horses from Betfred XML feed", EventLogEntryType.Information)

        ' Write to database
        Dim strResult As String
        gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Starting database update . . . . ", EventLogEntryType.Information)
        strResult = WriteEventList(bookmakerName, marketTypeCode, True)
        gobjEvent.WriteToEventLog("BookmakerHorseDbClass : Response from Database Update: : " + strResult, EventLogEntryType.Information)

    End Sub

    Public Sub MatchHorsesWithBookmakers(ByVal eventTypeId As Integer, ByVal marketTypeCode As String)
        '-----------------------------------------------------------------------*
        ' Sub Routine parameters                                                *
        ' -----------------------                                               *
        '   * eventTypeId   - Betfair eventTypeId e.g. 1=Soccer, 7=Horse Racing *
        '   * marketCode    - Betfair marketTypeCode e.g. MATCH_ODDS            *
        '-----------------------------------------------------------------------*
        Dim newMatched As MatchedEventClass

        Dim cno As MySqlConnection = New MySqlConnection(connectionString)
        Dim drBetOffer As MySqlDataReader
        Dim cmdBetOffer As New MySqlCommand

        ' /----------------------------------------------------------------\
        ' | MySql Select                                                   |
        ' | Get Spocosy betting odds                                       |
        ' \----------------------------------------------------------------/
        cmdBetOffer.CommandText = "SELECT be.`name`, be.`openDate`, be.`price`, be.`size`, be.`betName`, be.`marketName`, hre.`bookmakerName` AS provider_name, hre.`price`, hre.`eventDate`, hre.`meeting` FROM " &
                                                "betfair_event AS be, horse_racing_event AS hre " &
                                                "WHERE hre.betName = be.betName AND date(hre.eventDate) = date(be.openDate) AND be.marketTypeCode =@marketTypeCode " &
                                                "AND hre.eventDate >@nowDateTime"

        cmdBetOffer.Parameters.AddWithValue("marketTypeCode", marketTypeCode)

        ' Convert UTC Now to GMT (which actually handles BST aswell, confusing!)
        Dim saveUtcNow As DateTime = DateTime.UtcNow
        Dim saveGMTNow As DateTime = DateTime.UtcNow
        Dim gmt As TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")
        saveGMTNow = TimeZoneInfo.ConvertTimeFromUtc(saveUtcNow, gmt)
        cmdBetOffer.Parameters.AddWithValue("@nowDateTime", saveGMTNow)

        Try
            cno.Open()
            cmdBetOffer.Connection = cno
            drBetOffer = cmdBetOffer.ExecuteReader

            If drBetOffer.HasRows Then

                While drBetOffer.Read()

                    Dim strBetfairEventName As String = drBetOffer.GetString(0)
                    Dim dtBetfairOpenDate As DateTime = drBetOffer.GetDateTime(1)
                    Dim dbBetfairPrice As Double = drBetOffer.GetDouble(2)
                    Dim dbBetfairSize As Double = drBetOffer.GetDouble(3)
                    Dim strBetfairBetName As String = drBetOffer.GetString(4)
                    Dim strMarketName As String = drBetOffer.GetString(5)
                    Dim provider_name As String = drBetOffer.GetString(6)
                    Dim odds As Double = drBetOffer.GetDouble(7)
                    Dim dtEventDate As DateTime = drBetOffer.GetDateTime(8)
                    Dim strMeeting As String = drBetOffer.GetString(9)
                    Dim blnStore = True

                    ' Store the match
                    If blnStore Then

                        ' Convert date/time to string
                        Dim strEventDateTime As String = dtEventDate.ToString("yyyy-MM-dd HH:mm")
                        Dim strEventTime As String = strEventDateTime.Substring(11, 5)

                        Dim strMarketTypeCode As String = ""
                        Select Case marketTypeCode
                            Case "WIN"
                                strMarketTypeCode = "Winner"
                            Case "PLACE"
                                strMarketTypeCode = "Place"
                        End Select

                        ' Calculate rating 
                        Dim dblRating As Double = odds / dbBetfairPrice * 100

                        ' Resolve bookmaker name to image
                        Dim strBookmakerImageName = provider_name
                        strBookmakerImageName = strBookmakerImageName.Replace(" ", "_")
                        strBookmakerImageName = strBookmakerImageName.Replace(".", "_")
                        strBookmakerImageName = strBookmakerImageName.Replace("-", "_")
                        strBookmakerImageName = strBookmakerImageName.ToLower
                        Dim strBookmakerImage As String = "/images/" + strBookmakerImageName + ".gif"

                        'Create instance of Matched Event class
                        newMatched = New MatchedEventClass With {
                                         .openDate = dtEventDate,
                                         .eventTypeId = eventTypeId,
                                         .lay = dbBetfairPrice,
                                         .available = dbBetfairSize,
                                         .details = strMeeting + " " + strEventTime,
                                         .bookMaker = strBookmakerImage,
                                         .bookMakerName = provider_name,
                                         .bet = strBetfairBetName,
                                         .exchange = "/images/betfair_exchange.gif",
                                         .type = strMarketTypeCode,
                                         .back = odds,
                                         .rating = dblRating
                                        }

                        ' Add to list
                        matchedList.Add(newMatched)

                    End If

                End While

            Else

                ' Report no bets found

            End If

            drBetOffer.Close()
        Finally
            cno.Close()
        End Try


        '' Write to database
        Dim strResult As String
        gobjEvent.WriteToEventLog("BetFairDatabaseClass : Starting database update for matched_events . . . . ", EventLogEntryType.Information)
        strResult = WriteMatchedList(eventTypeId, marketTypeCode)
        gobjEvent.WriteToEventLog("BetFairDatabaseClass : Response from matched_events Database Update: : " + strResult, EventLogEntryType.Information)


    End Sub


    Private Function WriteEventList(bookmakerName As String, marketTypeCode As String, blnDeleteAll As Boolean) As String ''
        Dim cno As New MySqlConnection
        Dim cmd_del As New MySqlCommand
        Dim cmd As New MySqlCommand
        Dim SQLtrans As MySqlTransaction
        Dim num, num_del, i As Integer
        Dim msg As String = ""

        'Hard coding the connString this way is bad, but hopefully informative here.
        cno.ConnectionString = globalConnectionString

        '  CREATE TABLE `horse_racing_event` (
        '  `id` int(10) Not NULL AUTO_INCREMENT,
        '  `bookmakerName` varchar(50) Not NULL,
        '  `meeting` varchar(50) Not NULL,
        '  `race` varchar(150) Not NULL,
        '  `eventDate` datetime DEFAULT NULL,
        '  `marketTypeCode` varchar(50) DEFAULT NULL,
        '  `betName` varchar(150) DEFAULT NULL,
        '  `price` double(10,2) DEFAULT '0.00',
        '  `ut` timestamp Not NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        '  PRIMARY KEY(`id`),
        '  KEY `idx_bookmakerName` (`bookmakerName`),
        '  KEY `idx_eventDate` (`eventDate`),
        '  KEY `idx_betName` (`betName`)
        ') ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


        ' Establish delete command
        cmd_del.Connection = cno
        cmd_del.CommandText = "delete From `horse_racing_event` where bookmakerName =@bookmakerName And marketTypeCode =@marketTypeCode"
        cmd_del.Parameters.Add("@bookmakerName", MySqlDbType.String)
        cmd_del.Parameters.Add("@marketTypeCode", MySqlDbType.String)

        ' Establish insert command
        cmd.Connection = cno
        cmd.Parameters.Add("@bookmakerName", MySqlDbType.String)
        cmd.Parameters.Add("@meeting", MySqlDbType.String)
        cmd.Parameters.Add("@race", MySqlDbType.String)
        cmd.Parameters.Add("@eventDate", MySqlDbType.Timestamp)
        cmd.Parameters.Add("@marketTypeCode", MySqlDbType.String)
        cmd.Parameters.Add("@betName", MySqlDbType.String)
        cmd.Parameters.Add("@price", MySqlDbType.Double)
        cmd.CommandText = "INSERT INTO `oddsmatching`.`horse_racing_event`(`bookmakerName`,`meeting`,`race`,`eventDate`,`marketTypeCode`,`betName`,`price`)VALUES(@bookmakerName,@meeting,@race,@eventDate,@marketTypeCode,@betName,@price);"

        num = 0
        Try
            cno.Open()
            'Must open connection before starting transaction.
            SQLtrans = cno.BeginTransaction()
            cmd.Transaction = SQLtrans
            Try

                ' Delete all first at start of refresh
                If blnDeleteAll Then

                    'Ok, delete all rows first
                    cmd_del.Parameters("@bookmakerName").Value = bookmakerName
                    cmd_del.Parameters("@marketTypeCode").Value = marketTypeCode
                    num_del += cmd_del.ExecuteNonQuery

                End If

                'Ok, this is where the inserts really take place. All the stuff around
                'is just to prepare for this and handle errors that may occur.
                For i = 0 To eventList.Count - 1
                    cmd.Parameters("@bookmakerName").Value = eventList(i).bookmakerName
                    cmd.Parameters("@meeting").Value = eventList(i).meeting
                    cmd.Parameters("@race").Value = eventList(i).race
                    cmd.Parameters("@eventDate").Value = eventList(i).eventTimestamp
                    cmd.Parameters("@marketTypeCode").Value = eventList(i).marketTypeCode
                    cmd.Parameters("@betName").Value = eventList(i).betName
                    cmd.Parameters("@price").Value = eventList(i).price
                    num += cmd.ExecuteNonQuery
                Next i
                'We are done. Now commit the transaction - actually change the DB.
                SQLtrans.Commit()
            Catch e1 As System.Exception
                'If anything went wrong attempt to rollback transaction
                Try
                    SQLtrans.Rollback()
                Catch e2 As System.Exception
                    'This is where you will be if the write went wrong AND the rollback failed.
                    'It's a bad place to be: Unable to rollback transaction - this REALLY hurts...
                    msg += "Unable To rollback transaction. " & e2.Message
                End Try
                msg += "Insert failed, transaction rolled back. " & e1.Message
            End Try
        Catch e3 As System.Exception
            msg += "Insert failed, might be unable To open connection. " & e3.Message
        Finally
            Try
                'Whatever happens, you will land here and attempt to close the connection.
                cno.Close()
            Catch e4 As System.Exception
                'If closing the connection goes wrong...
                msg += "I can't close connection. " & e4.Message
            End Try
        End Try

        msg += "Deleted rows : " + num_del.ToString + " Inserted rows : " + num.ToString
        Return msg

    End Function

    Private Function WriteMatchedList(eventTypeId As Integer, marketTypeCode As String) As String ''
        Dim cno As New MySqlConnection
        Dim cmd_del As New MySqlCommand
        Dim cmd As New MySqlCommand
        Dim SQLtrans As MySqlTransaction
        Dim num, num_del, i As Integer
        Dim msg As String = ""

        'Hard coding the connString this way is bad, but hopefully informative here.
        cno.ConnectionString = globalConnectionString

        ' Establish delete command
        cmd_del.Connection = cno
        cmd_del.CommandText = "delete From `matched_event` where betfairEventTypeId =@eventTypeId And betfairMarketTypeCode =@marketTypeCode"
        cmd_del.Parameters.Add("@eventTypeId", MySqlDbType.Int16)
        cmd_del.Parameters.Add("@marketTypeCode", MySqlDbType.String)

        ' Establish insert command
        cmd.Connection = cno
        cmd.Parameters.Add("@eventDate", MySqlDbType.Timestamp)
        cmd.Parameters.Add("@sport", MySqlDbType.String)
        cmd.Parameters.Add("@details", MySqlDbType.String)
        cmd.Parameters.Add("@betName", MySqlDbType.String)
        cmd.Parameters.Add("@marketName", MySqlDbType.String)
        cmd.Parameters.Add("@rating", MySqlDbType.Double)
        cmd.Parameters.Add("@info", MySqlDbType.String)
        cmd.Parameters.Add("@bookmaker", MySqlDbType.String)
        cmd.Parameters.Add("@bookmaker_name", MySqlDbType.String)
        cmd.Parameters.Add("@back", MySqlDbType.Double)
        cmd.Parameters.Add("@exchange", MySqlDbType.String)
        cmd.Parameters.Add("@lay", MySqlDbType.Double)
        cmd.Parameters.Add("@size", MySqlDbType.Double)
        cmd.Parameters.Add("@betfairEventTypeId", MySqlDbType.Int16)
        cmd.Parameters.Add("@betfairMarketTypeCode", MySqlDbType.String)
        cmd.Parameters.Add("@competitionName", MySqlDbType.String)
        cmd.Parameters.Add("@countryCode", MySqlDbType.String)
        cmd.Parameters.Add("@timezone", MySqlDbType.String)

        cmd.CommandText = "INSERT INTO `matched_event` (`eventDate`,`sport`,`details`,`betName`,`marketName`,`rating`,`info`,`bookmaker`,`bookmaker_name`,`back`,`exchange`,`lay`,`size`,`betfairEventTypeId`,`betfairMarketTypeCode`,`competitionName`,`countryCode`,`timezone`) VALUES (@eventDate,@sport,@details,@betName,@marketName,@rating,@info,@bookmaker,@bookmaker_name,@back,@exchange,@lay,@size,@betfairEventTypeId,@betfairMarketTypeCode,@competitionName,@countryCode,@timezone)"

        num = 0
        Try
            cno.Open()
            'Must open connection before starting transaction.
            SQLtrans = cno.BeginTransaction()
            cmd.Transaction = SQLtrans
            Try

                'Ok, delete all rows first
                cmd_del.Parameters("@eventTypeId").Value = eventTypeId
                cmd_del.Parameters("@marketTypeCode").Value = marketTypeCode
                num_del += cmd_del.ExecuteNonQuery

                'Ok, this is where the inserts really take place. All the stuff around
                'is just to prepare for this and handle errors that may occur.
                For i = 0 To matchedList.Count - 1

                    cmd.Parameters("@eventDate").Value = matchedList(i).openDate
                    If eventTypeId = 1 Then
                        cmd.Parameters("@sport").Value = "/images/football.png"
                    ElseIf eventTypeId = 7 Then
                        cmd.Parameters("@sport").Value = "/images/horse.png"
                    End If
                    cmd.Parameters("@details").Value = matchedList(i).details
                    cmd.Parameters("@betName").Value = matchedList(i).bet
                    cmd.Parameters("@marketName").Value = matchedList(i).type
                    cmd.Parameters("@rating").Value = matchedList(i).rating
                    cmd.Parameters("@info").Value = "/images/info.png"
                    cmd.Parameters("@bookmaker").Value = matchedList(i).bookMaker
                    cmd.Parameters("@bookmaker_name").Value = matchedList(i).bookMakerName
                    cmd.Parameters("@back").Value = matchedList(i).back
                    cmd.Parameters("@exchange").Value = matchedList(i).exchange
                    cmd.Parameters("@lay").Value = matchedList(i).lay
                    cmd.Parameters("@size").Value = matchedList(i).available
                    cmd.Parameters("@betfairEventTypeId").Value = matchedList(i).eventTypeId
                    cmd.Parameters("@betfairMarketTypeCode").Value = marketTypeCode
                    cmd.Parameters("@competitionName").Value = "tbc"
                    cmd.Parameters("@countryCode").Value = "GB"
                    cmd.Parameters("@timezone").Value = "tbc"

                    num += cmd.ExecuteNonQuery
                Next i
                'We are done. Now commit the transaction - actually change the DB.
                SQLtrans.Commit()
            Catch e1 As System.Exception
                'If anything went wrong attempt to rollback transaction
                Try
                    SQLtrans.Rollback()
                Catch e2 As System.Exception
                    'This is where you will be if the write went wrong AND the rollback failed.
                    'It's a bad place to be: Unable to rollback transaction - this REALLY hurts...
                    msg += "Unable To rollback transaction. " & e2.Message
                End Try
                msg += "Insert failed, transaction rolled back. " & e1.Message
            End Try
        Catch e3 As System.Exception
            msg += "Insert failed, might be unable To open connection. " & e3.Message
        Finally
            Try
                'Whatever happens, you will land here and attempt to close the connection.
                cno.Close()
            Catch e4 As System.Exception
                'If closing the connection goes wrong...
                msg += "I can't close connection. " & e4.Message
            End Try
        End Try

        msg += "Deleted rows : " + num_del.ToString + " Inserted rows : " + num.ToString
        Return msg

    End Function
    Public Function CheckValidCourse(courseName As String) As Boolean
        Dim blnReturn As Boolean
        Dim strCourseListUk As String = "Aintree,Ascot,Ayr,Bangor-On-Dee,Bath,Beverley,Brighton,Carlisle,Cartmel,Catterick,Chelmsford City,Cheltenham,Chepstow,Chester,Doncaster,Epsom Downs,Exeter,Fakenham,Ffos Las, Fontwell,Goodwood,Hamilton,Haydock,Hexham,Huntingdon,Kelso,Kempton,Leicester,Lingfield,Ludlow,Market Rasen,Musselburgh,Newbury,Newcastle,Newmarket,Newton Abbot,Nottingham,Perth,Plumpton,Pontefract,Redcar,Ripon,Salisbury,Sandown,Sedgefield,Southwell,Stratford,Taunton,Thirsk,Towcester,Uttoxeter,Warwick,Wetherby,Wincanton,Windsor,Wolverhampton,Worcester,Yarmouth,York"
        Dim strCourseListEire As String = "Ballinrobe,Bellewstown,Clonmel,Cork,Curragh,Dundalk,Fairyhouse,Galway,Gowran Park,Kilbeggan,Killarney,Laytown,Leopardstown,Limerick,Listowel,Naas,Navan,Punchestown,Roscommon,Sligo,Thurles,Tipperary,Tralee,Tramore,Wexford"
        If strCourseListUk.IndexOf(courseName) >= 0 Or strCourseListEire.IndexOf(courseName) >= 0 Then
            blnReturn = True
        Else
            blnReturn = False
        End If
        Return blnReturn
    End Function

End Class
