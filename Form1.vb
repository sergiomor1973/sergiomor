Imports System.IO.Ports
Imports System.Threading
Imports System.Management
Imports System.Collections.Concurrent

Public Class Form1
    Private SerialPort1 As New SerialPort()
    Private running As Boolean = False
    Private logQueue As New ConcurrentQueue(Of String)()
    Private monitorThread As Thread

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim logThread As New Thread(AddressOf LogWorker)
        logThread.IsBackground = True
        logThread.Start()
    End Sub

    Private Sub BtnServiceINI_Click(sender As Object, e As EventArgs) Handles BtnServiceINI.Click
        LogAsync("Iniciando rotina de detecção do Interface")
        running = True
        monitorThread = New Thread(AddressOf MonitorarCP2102)
        monitorThread.IsBackground = True
        monitorThread.Start()
    End Sub

    Private Sub MonitorarCP2102()
        While running
            Dim portaDetectada As String = DetectarPortaCP2102()
            If Not String.IsNullOrEmpty(portaDetectada) Then
                Try
                    Dim NomeDisp As String = ObterNomeDispositivo(portaDetectada)
                    LogAsync("Interface; " & NomeDisp)
                    ConfigurarPortaSerial(portaDetectada)
                    LogAsync("Porta detectada e aberta: " & SerialPort1.PortName)
                    LoopComunicacao()
                Catch ex As Exception
                    LogAsync("Erro ao abrir porta: " & ex.Message)
                End Try
            Else
                LogAsync("Aguardando comunicação...")
            End If
            Thread.Sleep(500)
        End While
    End Sub

    Private Sub ConfigurarPortaSerial(porta As String)
        With SerialPort1
            If .IsOpen Then
                .Close()
            End If
            .PortName = porta
            .BaudRate = 19200
            .DataBits = 8
            .Parity = Parity.Even
            .StopBits = StopBits.Two
            .Open()
            Thread.Sleep(60)
            .DiscardInBuffer()
            .DiscardOutBuffer()
        End With
    End Sub

    Private Sub LoopComunicacao()
        LogAsync("Entrando no loop de comunicação ativa com a PSP...")
        Dim MsgCmd81_first As Integer = 0
        While running
            Try
                'Thread.Sleep(1)
                If SerialPort1.IsOpen AndAlso SerialPort1.BytesToRead > 0 Then
                    Dim packet As Byte() = ReadPacket(&H5A)
                    If packet IsNot Nothing Then
                        ' Monta comando em hex para logging
                        Dim comandoHex As String = BitConverter.ToString(packet).Replace("-", " ")
                        LogAsync("PSP-Bat: " & comandoHex)

                        Dim respostaBytes As Byte() = ProcessPSPCommand(packet)
                        SerialPort1.Write(respostaBytes, 0, respostaBytes.Length)
                        'Thread.Sleep(1)
                        Dim resposta As String = BitConverter.ToString(respostaBytes).Replace("-", " ")
                        LogAsync("Bat-PSP: " & resposta)

                        If packet(2) = &H81 AndAlso MsgCmd81_first = 0 Then
                            MsgCmd81_first = MsgCmd81_first + 1
                            LogAsync("A PSP está em Service Mode." & vbCrLf & "Pode continuar a ver os logs se desejar.")
                        End If
                    End If
                End If

            Catch ex As Exception
                LogAsync("Porta desconectada. A tentar reconectar em 1 segundo...")
                Try
                    If SerialPort1.IsOpen Then SerialPort1.Close()
                Catch
                End Try
                Thread.Sleep(1000)
                ReconectarAteSucesso()
                Exit While
            End Try
        End While
    End Sub

    ' Função que lê um pacote completo como o readpacket(key) do Python
    Private Function ReadPacket(key As Byte) As Byte()
        Dim packet As New List(Of Byte)()

        ' 1. Lê o cabeçalho
        Dim header As Integer = SerialPort1.ReadByte()
        If header <> key Then Return Nothing
        packet.Add(CByte(header))

        ' 2. Lê o comprimento
        While SerialPort1.BytesToRead = 0
            Thread.Sleep(1)
        End While
        Dim lenByte As Integer = SerialPort1.ReadByte()
        packet.Add(CByte(lenByte))

        ' 3. Lê o opcode
        While SerialPort1.BytesToRead = 0
            Thread.Sleep(1)
        End While
        Dim opcode As Integer = SerialPort1.ReadByte()
        packet.Add(CByte(opcode))

        ' 4. Corpo da mensagem (se aplicável)
        Dim msgLen As Integer = lenByte - 2
        If msgLen > 0 Then
            ' Espera até todos os bytes do corpo + checksum estarem disponíveis
            While SerialPort1.BytesToRead < (msgLen + 1)
                Thread.Sleep(1)
            End While
            Dim corpo(msgLen - 1) As Byte
            SerialPort1.Read(corpo, 0, msgLen)
            packet.AddRange(corpo)
        End If

        ' 5. Lê o checksum
        Dim csum As Integer = SerialPort1.ReadByte()
        packet.Add(CByte(csum))

        Return packet.ToArray()
    End Function

    Private Sub ReconectarAteSucesso()
        While running
            Dim portaDetectada As String = DetectarPortaCP2102()
            If Not String.IsNullOrEmpty(portaDetectada) Then
                Try
                    ConfigurarPortaSerial(portaDetectada)
                    LogAsync("Reconectado à porta: " & portaDetectada)
                    LoopComunicacao()
                    Exit Sub
                Catch ex As Exception
                    LogAsync("Erro ao reabrir porta: " & ex.Message)
                End Try
            End If
            Thread.Sleep(1000)
        End While
    End Sub

    Private Function DetectarPortaCP2102() As String
        Try
            Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_SerialPort")
            For Each obj As ManagementObject In searcher.Get()
                Dim nome As String = obj("Name").ToString()
                If nome.ToLower().Contains("cp210") Then
                    Return obj("DeviceID").ToString()
                End If
            Next
        Catch
        End Try
        Return String.Empty
    End Function

    Private Function ObterNomeDispositivo(porta As String) As String
        Dim nomeDispositivo As String = String.Empty
        Try
            Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_SerialPort")
            For Each obj As ManagementObject In searcher.Get()
                If obj("DeviceID").ToString().Contains(porta) Then
                    nomeDispositivo = obj("Name").ToString()
                    Exit For
                End If
            Next
        Catch
        End Try
        Return nomeDispositivo
    End Function

    Private Sub LogAsync(msg As String)
        logQueue.Enqueue($"{DateTime.Now:HH:mm:ss.fff} - {msg}")
    End Sub

    Private Sub LogWorker()
        While True
            Dim mensagem As String = String.Empty
            If logQueue.TryDequeue(mensagem) Then
                If TxtCom.InvokeRequired Then
                    TxtCom.Invoke(Sub() TxtCom.AppendText(mensagem & Environment.NewLine))
                Else
                    TxtCom.AppendText(mensagem & Environment.NewLine)
                End If
            End If
            Thread.Sleep(10)
        End While
    End Sub

    Private Function HexStringToBytes(hex As String) As Byte()
        Dim bytes(hex.Length \ 2 - 1) As Byte
        For i As Integer = 0 To bytes.Length - 1
            bytes(i) = Convert.ToByte(hex.Substring(i * 2, 2), 16)
        Next
        Return bytes
    End Function

    Private Sub BtnStopService_Click(sender As Object, e As EventArgs) Handles BtnStopService.Click
        running = False
        Try
            If SerialPort1.IsOpen Then
                SerialPort1.Close()
                LogAsync("Serviço parado e porta de série fechada.")
            Else
                LogAsync("Serviço parado.")
            End If
        Catch ex As Exception
            LogAsync("Erro ao fechar porta: " & ex.Message)
        End Try
    End Sub

    Private Sub BtnClearMonitor_Click(sender As Object, e As EventArgs) Handles BtnClearMonitor.Click
        TxtCom.Clear()
    End Sub

    Private Sub BtnEncAES_Click(sender As Object, e As EventArgs) Handles BtnEncAES.Click
        Dim PrepMsg As String

        TxtOutput.Clear()
        TxtOutput2.Clear()

        Try
            PrepMsg = Replace(TxtInput.Text, " ", "")
            If TxtInput.Text <> "" Then
                TxtInput.Text = PrepMsg
                Dim Cmd802Enc() As Byte = HexStringToBytes(TxtInput.Text)
                If Cmd802Enc(0) <> &H5A Then Exit Sub
                If Cmd802Enc(2) <> &H80 Then Exit Sub
                Dim Cmd80Resp() As Byte = ProcessPSPCommand(Cmd802Enc)
                TxtOutput.Text = BitConverter.ToString(Cmd80Resp).Replace("-", "")
            End If
            PrepMsg = Replace(TxtInput2.Text, " ", "")
            If TxtInput2.Text <> "" Then
                TxtInput2.Text = PrepMsg
                Dim Cmd812Enc() As Byte = HexStringToBytes(TxtInput2.Text)
                If Cmd812Enc(0) <> &H5A Then Exit Sub
                If Cmd812Enc(2) <> &H81 Then Exit Sub
                Dim Cmd81Resp() As Byte = ProcessPSPCommand(Cmd812Enc)
                TxtOutput2.Text = BitConverter.ToString(Cmd81Resp).Replace("-", "")
            End If
        Catch ex As Exception

        End Try
    End Sub
End Class
