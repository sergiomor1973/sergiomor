Option Explicit On
Imports System.IO.Ports  ' Para a comunicação serial
Imports System.Text      ' Para manipulação de strings
Imports System.Security.Cryptography  ' Para a criptografia AES
Imports System.Threading  ' Para controles de espera e sincronização

Module Module1
    Private version As Integer
    Private challenge1b() As Byte


    ' Dicionários para chaves (Agora globais)
    Private keystore As New Dictionary(Of Integer, String)
    Private challenge1_secret As New Dictionary(Of Integer, String)
    Private challenge2_secret As New Dictionary(Of Integer, String)

    ' Chaves GO
    Private go_key1() As Byte
    Private go_key2() As Byte
    Private go_secret() As Byte

    ' Inicialização
    Public Sub InitializeVariables()
        ' Preenchendo os dicionários globais
        keystore = New Dictionary(Of Integer, String) From {
            {0, "5C52D91CF382ACA489D88178EC16297B"},
            {1, "9D4F50FCE1B68E1209307DDBA6A5B5AA"},
            {2, "0975988864ACF7621BC0909DF0FCABFF"},
            {3, "C9115CE2064A2686D8D6D9D08CDE3059"},
            {4, "667539D2FB4273B2903FD7A39ED2C60C"},
            {5, "F4FAEF20F4DBAB31D18674FD8F990566"},
            {6, "EA0C811363D7E930F961135A4F352DDC"},
            {8, "0A2E73305C382D4F310D0AED84A41800"},
            {9, "D20474308FE269046ED7BB07CF1CFF43"},
            {10, "AC00C0E3E80AF0683FDD1745194543BD"},
            {11, "0177D750BDFD2BC1A0493A134A4C6ACF"},
            {12, "05349170939345EE951A14843334A0DE"},
            {13, "DFF3FCD608B05597CF09A23BD17D3FD2"},
            {47, "4AA7C7B01134466FAC82163E4BB51BF9"},
            {151, "CAC8B87ACD9EC49690ABE0813920B110"},
            {179, "03BEB65499140483BA187A64EF90261D"},
            {217, "C7AC1306DEFE39EC83A1483B0EE2EC89"},
            {235, "418499BE9D35A3B9FC6AD0D6F041BB26"}
        }

        challenge1_secret = New Dictionary(Of Integer, String) From {
            {0, "D2072253A4F27468"},
            {1, "B37A16EF557BD089"},
            {2, "A04E32BBA7139E46"},
            {3, "B0B809833989FAE2"},
            {4, "FE7D7899BFEC47C5"},
            {5, "306F3A03D86CBEE4"},
            {6, "8422DFEAE21B63C2"},
            {8, "AD4043B256EB458B"},
            {10, "C2377E8A74096C5F"},
            {13, "581C7F1944F96262"},
            {47, "F1BC562BD55BB077"},
            {151, "AF6010A846F741F3"},
            {179, "DBD3AEA4DB046410"},
            {217, "90E1F0C00178E3FF"},
            {235, "0BD9027E851FA123"}
        }

        challenge2_secret = New Dictionary(Of Integer, String) From {
            {0, "F5D7D4B575F08E4E"},
            {1, "CC699581FD89126C"},
            {2, "495E034794931D7B"},
            {3, "F4E04313AD2EB4DB"},
            {4, "865E3EEF9DFBB1FD"},
            {5, "FF72BD2B83B89D2F"},
            {6, "58B95AAEF399DBD0"},
            {8, "67C07215D96B39A1"},
            {10, "093EC519AF0F502D"},
            {13, "318053875C203E24"},
            {47, "1BDF2433EB29155B"},
            {151, "9DEEC01144B66F41"},
            {179, "E32B8F56B2641298"},
            {217, "C34A6A7B205FE8F9"},
            {235, "F791ED0B3F49A448"}
        }

        ' GO keys
        go_key1 = HexStringToBytes("C66E9ED6ECBCB121B7465D25037D6646")
        go_key2 = HexStringToBytes("DA24DAB43A61CBDF61FD255D0AEA7957")
        go_secret = HexStringToBytes("880E2A94110926B20E53E22AE648AE9D")
    End Sub

    Public Function HexStringToBytes(hexString As String) As Byte()
        Return Enumerable.Range(0, hexString.Length \ 2).
           Select(Function(i) Convert.ToByte(hexString.Substring(i * 2, 2), 16)).
           ToArray()
    End Function

    ' Conversão de array de bytes para string hex
    Public Function BytesToHex(bytes() As Byte) As String
        Return BitConverter.ToString(bytes).Replace("-", "")
    End Function

    Public Function MatrixSwap(key() As Byte) As Byte()
        Dim newmap As Integer() = {0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15}
        Dim temp(15) As Byte

        For i As Integer = 0 To 15
            temp(i) = key(newmap(i))
        Next

        Return temp
    End Function

    Public Function MixChallenge1(version As Integer, challenge() As Byte) As Byte()
        Dim secret() As Byte = HexStringToBytes(challenge1_secret(version))
        Dim data(15) As Byte

        data(0) = secret(0) : data(4) = secret(1) : data(8) = secret(2) : data(12) = secret(3)
        data(1) = secret(4) : data(5) = secret(5) : data(9) = secret(6) : data(13) = secret(7)
        data(2) = challenge(0) : data(6) = challenge(1) : data(10) = challenge(2) : data(14) = challenge(3)
        data(3) = challenge(4) : data(7) = challenge(5) : data(11) = challenge(6) : data(15) = challenge(7)

        Return data
    End Function

    Public Function MixChallenge2(version As Integer, challenge() As Byte) As Byte()
        Dim secret() As Byte = HexStringToBytes(challenge2_secret(version))
        Dim data(15) As Byte

        data(0) = challenge(0) : data(4) = challenge(1) : data(8) = challenge(2) : data(12) = challenge(3)
        data(1) = challenge(4) : data(5) = challenge(5) : data(9) = challenge(6) : data(13) = challenge(7)
        data(2) = secret(0) : data(6) = secret(1) : data(10) = secret(2) : data(14) = secret(3)
        data(3) = secret(4) : data(7) = secret(5) : data(11) = secret(6) : data(15) = secret(7)

        Return data
    End Function

    Public Function CombineBytes(arr1() As Byte, arr2() As Byte) As Byte()
        Dim result(arr1.Length + arr2.Length - 1) As Byte

        Array.Copy(arr1, 0, result, 0, arr1.Length)
        Array.Copy(arr2, 0, result, arr1.Length, arr2.Length)

        Return result
    End Function

    Public Function MidByteArray(arr() As Byte, start As Integer, Optional length As Integer = -1) As Byte()
        If length = -1 Then length = arr.Length - start
        Dim result(length - 1) As Byte

        Array.Copy(arr, start, result, 0, length)
        Return result
    End Function

    Public Function AES_ECB_Encrypt(data() As Byte, key() As Byte) As Byte()
        Using aes As Aes = Aes.Create()
            aes.Mode = CipherMode.ECB
            aes.Padding = PaddingMode.PKCS7
            aes.Key = key

            Using encryptor As ICryptoTransform = aes.CreateEncryptor()
                Return encryptor.TransformFinalBlock(data, 0, data.Length)
            End Using
        End Using
    End Function


    Public Function Checksum(packet() As Byte) As Byte
        Dim soma As Integer = 0

        For Each value As Byte In packet
            soma += value
        Next

        Return CByte((255 - (soma Mod 256)) Mod 256)
    End Function

    Public Function ProcessPSPCommand(cmdBytes As Byte()) As Byte()
        If keystore.Count = 0 Then InitializeVariables()


        Dim resposta() As Byte
        Dim screq() As Byte, req() As Byte, data() As Byte
        Dim challenge1a() As Byte, response1() As Byte
        Dim sn() As Byte = {&HFF, &HFF, &HFF, &HFF}
        Dim cks As Byte
        Dim cksArr(0) As Byte

        Select Case cmdBytes(2)
            Case &H1 : resposta = {&HA5, &H5, &H6, &H10, &HC3, &H6, &H76}
            Case &H2 : resposta = {&HA5, &H3, &H6, &H1B, &H36}   ' Modelo da bateria
            Case &H3 : resposta = {&HA5, &H4, &H6, &H68, &H10, &HD8} ' Status da bateria
            Case &H4 : resposta = {&HA5, &H4, &H6, &HE2, &H4, &H6A} ' Capacidade restante
            Case &H7 : resposta = {&HA5, &H4, &H6, &H8, &H7, &H41} ' Temperatura
            Case &H8 : resposta = {&HA5, &H4, &H6, &HE2, &H4, &H6A}
            Case &H9 : resposta = {&HA5, &H4, &H6, &H1, &H4, &H4B} ' Voltagem
            Case &HB : resposta = {&HA5, &H4, &H6, &HF, &H0, &H41} ' Dados de carga
            Case &HD : resposta = {&HA5, &H7, &H6, &H9D, &H10, &H10, &H28, &H14, &H54}
            Case &H16 : resposta = {&HA5, &H13, &H6, &H53, &H6F, &H6E, &H79, &H45, &H6E}


            Case &HC
                resposta = CombineBytes(HexStringToBytes("A50606"), sn)
                cks = Checksum(resposta)
                cksArr(0) = cks
                resposta = CombineBytes(resposta, cksArr)

            Case &H80
                screq = MidByteArray(cmdBytes, 3)
                version = screq(0)

                If Not keystore.ContainsKey(version) Then
                    response1 = {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF}

                Else
                    req = MidByteArray(screq, 1, 8)
                    data = MixChallenge1(version, req)
                    challenge1a = AES_ECB_Encrypt(MatrixSwap(data), HexStringToBytes(keystore(version)))

                    Dim second(15) As Byte
                    Array.Copy(challenge1a, second, 16)

                    challenge1b = MatrixSwap(AES_ECB_Encrypt(second, HexStringToBytes(keystore(version))))
                    response1 = CombineBytes(MidByteArray(challenge1a, 0, 8), MidByteArray(challenge1b, 0, 8))
                End If

                resposta = CombineBytes(HexStringToBytes("A51206"), response1)
                cks = Checksum(resposta)
                cksArr(0) = cks
                resposta = CombineBytes(resposta, cksArr)

            Case &H81
                screq = MidByteArray(cmdBytes, 3, 8)
                If version = 0 Then
                    resposta = {&HA5, &H12, &H6, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF}
                Else
                    Dim challenge1b8() As Byte = MidByteArray(challenge1b, 0, 8)
                    data = MixChallenge2(version, challenge1b8)
                    data = MatrixSwap(data)

                    Dim response2() As Byte = AES_ECB_Encrypt(data, HexStringToBytes(keystore(version)))
                    response2 = AES_ECB_Encrypt(response2, HexStringToBytes(keystore(version)))

                    response2 = MidByteArray(response2, 0, 16)
                    resposta = CombineBytes(HexStringToBytes("A51206"), response2)
                End If

                cks = Checksum(resposta)
                cksArr(0) = cks
                resposta = CombineBytes(resposta, cksArr)

            Case Else
                resposta = HexStringToBytes("A50006")
        End Select

        Return resposta


    End Function


End Module
