Imports System.IO.Ports
Imports System.Threading
Imports System.Management
Imports System.Text
Imports System.Collections.Concurrent

Public Class Form1
    Private SerialPort1 As New SerialPort()
    Private running As Boolean = False
    Private monitorThread As Thread
    Private reconexaoTimer As System.Windows.Forms.Timer
    Private cp2102Detectado As Boolean = False
    Private logQueue As New ConcurrentQueue(Of String)()

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        reconexaoTimer = New System.Windows.Forms.Timer()
        reconexaoTimer.Interval = 5
        AddHandler reconexaoTimer.Tick, AddressOf VerificarReconexao
    End Sub

    ' **Botão para iniciar o processo**
    Private Sub BtnServiceINI_Click(sender As Object, e As EventArgs) Handles BtnServiceINI.Click
        LogMessage("Iniciando rotina de detecção do Interface")

        running = True
        monitorThread = New Thread(AddressOf MonitorarCP2102)
        monitorThread.Start()
    End Sub

    ' **Loop contínuo que monitoriza o CP2102**
    Private Sub MonitorarCP2102()
        While running
            Dim portaDetectada As String = DetectarPortaCP2102()

            If Not String.IsNullOrEmpty(portaDetectada) Then
                If Not SerialPort1.IsOpen Then
                    Try
                        Dim NomeDisp As String = ObterNomeDispositivo(portaDetectada)
                        LogMessage("Interface; " & NomeDisp)
                        LerDadosSerialCOM4(portaDetectada)
                        With SerialPort1
                            .PortName = portaDetectada
                            .BaudRate = 19200 '19200
                            .DataBits = 8
                            .Parity = Parity.Even
                            .StopBits = StopBits.Two

                            '.readTimeout = 5
                            '.WriteTimeout = 5
                            '.Handshake = Handshake.None
                            '.RtsEnable = False
                            '.DtrEnable = False

                            .Open()
                        End With
                        Threading.Thread.Sleep(60)
                        cp2102Detectado = True
                        LogMessage("Porta detectada e aberta: " & SerialPort1.PortName)
                        reconexaoTimer.Stop()

                        IniciarLoopConversacao()
                    Catch ex As Exception
                        LogMessage("Erro ao abrir porta: " & ex.Message)
                    End Try
                End If
            Else
                If cp2102Detectado Then
                    LogMessage("Conexão perdida! Tentando reconectar...")
                    cp2102Detectado = False
                    reconexaoTimer.Start()
                End If
            End If

            'Thread.Sleep(500)
        End While
    End Sub



    ' **Loop de comunicação ativa com a PSP**
    Private Sub IniciarLoopConversacao()
        Dim receivedBuffer As New List(Of Byte)()
        LogMessage("Entrando no loop de comunicação ativa com a PSP...")

        While running And SerialPort1.IsOpen

            Try
                Thread.Sleep(1)

                If SerialPort1.BytesToRead > 0 Then
                    Dim buffer(SerialPort1.BytesToRead - 1) As Byte
                    SerialPort1.Read(buffer, 0, buffer.Length)
                    receivedBuffer.AddRange(buffer)



                    '*
                    While receivedBuffer.Count >= 4 AndAlso receivedBuffer(0) = &H5A
                        ' Garantimos que apenas um comando completo seja processado
                        Dim comandoHex As String = BitConverter.ToString(buffer).Replace("-", "")
                            LogMessage("Comando recebido da PSP: " & comandoHex)
                        SerialPort1.DiscardInBuffer()
                        SerialPort1.DiscardOutBuffer()
                        Dim resposta As String = ProcessPSPCommand(comandoHex)

                            Dim respostaBytes As Byte() = HexStringToBytes(resposta)

                            SerialPort1.Write(respostaBytes, 0, respostaBytes.Length)

                        LogMessage("Resposta enviada para a PSP: " & resposta)
                    End While
                    '*
                End If
            Catch ex As Exception
                LogMessage("Erro na comunicação com a PSP: " & ex.Message)
            End Try

        End While
    End Sub

    ' **Timer que verifica reconexão a cada 5ms**
    Private Sub VerificarReconexao(sender As Object, e As EventArgs)
        Dim portaDetectada As String = DetectarPortaCP2102()
        If Not String.IsNullOrEmpty(portaDetectada) Then
            LogMessage("Reconexão bem-sucedida! Porta aberta novamente: " & portaDetectada)
            reconexaoTimer.Stop()
            IniciarLoopConversacao()
        Else
            LogMessage("Nenhuma PSP detectada! Comunicação encerrada.")
            SerialPort1.DiscardInBuffer() ' Limpa o buffer para evitar respostas indevidas
        End If
    End Sub

    ' **Botão para parar o serviço**
    Private Sub BtnStopService_Click(sender As Object, e As EventArgs) Handles BtnStopService.Click
        running = False
        reconexaoTimer.Stop()

        If SerialPort1.IsOpen Then
            SerialPort1.Close()
            LogMessage("Serviço parado e porta serial fechada.")
        Else
            LogMessage("Serviço parado.")
        End If
    End Sub

    ' **Função para detetar a porta serial do CP2102**
    Private Function DetectarPortaCP2102() As String
        Dim portasDisponiveis() As String = SerialPort.GetPortNames()
        For Each porta As String In portasDisponiveis
            If porta.Contains("COM") Then Return porta
        Next
        Return String.Empty
    End Function

    Private Function ObterNomeDispositivo(porta As String) As String
        Dim nomeDispositivo As String = ""

        ' Consultar informações do dispositivo
        Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_SerialPort")
        For Each obj As ManagementObject In searcher.Get()
            If obj("DeviceID").ToString().Contains(porta) Then
                nomeDispositivo = obj("Name").ToString()
                Exit For
            End If
        Next

        Return nomeDispositivo
    End Function

    ' **Função para registar logs na interface**
    Private Sub LogMessage(msg As String)
        If TxtCom.InvokeRequired Then
            TxtCom.Invoke(Sub() TxtCom.AppendText(msg & Environment.NewLine))
        Else
            TxtCom.AppendText(msg & Environment.NewLine)
        End If
    End Sub









    Public Function LerDadosSerialCOM4(Porta As String) As String
        Dim dadosRecebidos As String = ""
        Dim Envio As String
        Dim SerialPort1 As New SerialPort()

        Try
            With SerialPort1
                .PortName = Porta
                .BaudRate = 19200 '19200,38400
                .DataBits = 8
                .Parity = Parity.Even
                .StopBits = StopBits.Two
                .ReadTimeout = 1000 ' 1 segundo
                .WriteTimeout = 1000
                .Handshake = Handshake.None
                .RtsEnable = False
                .DtrEnable = False

                .Open()
            End With

            ' Leitura de dados (exemplo: lê até receber uma linha ou até ocorrer timeout)
            Try
                dadosRecebidos = "The dog is my friend 123." & vbCrLf
                Envio = "The dog is my friend 123." & vbCrLf
                SerialPort1.WriteLine(Envio)
                dadosRecebidos = SerialPort1.ReadLine()
                If dadosRecebidos = Envio Then
                    LogMessage("Ping correto")
                End If
            Catch ex As TimeoutException
                dadosRecebidos = "Falha no ping"
            End Try

        Catch ex As Exception
            dadosRecebidos = "Erro: " & ex.Message
        Finally
            If SerialPort1.IsOpen Then
                SerialPort1.Close()
            End If
        End Try

        Return dadosRecebidos
    End Function

    Private Function HexStringToBytes(hex As String) As Byte()
        Dim bytes(hex.Length \ 2 - 1) As Byte
        For i As Integer = 0 To bytes.Length - 1
            bytes(i) = Convert.ToByte(hex.Substring(i * 2, 2), 16)
        Next
        Return bytes
    End Function

    Private Sub BtnClearMonitor_Click(sender As Object, e As EventArgs) Handles BtnClearMonitor.Click
        TxtCom.Clear()
    End Sub


End Class