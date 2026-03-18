## VMC Replace Avatar
VRCアバターから変換して出力したアバターデータを読み込み、VRMと置き換えるVMCMod  
BeatSaber用アバターとして使用することを想定しています。
  
### 必要なもの
・VirtualMotion Capture  
[https://vmc.info/](https://vmc.info/)  
  
・AvatarExporter(アバターデータ出力用)  
[AvatarExporter]

・VMC Sport（VMCMod）  
[https://github.com/Snow1226/VMCSpout](https://github.com/Snow1226/VMCSpout)  

### 使い方  
Avatar Changeボタンを押して、事前にVRC用Projectから出力したアバターをロードしてください。  
※現状キャリブレーション前に呼び出さないと位置が正常になりません。  
  
### 設定
<img width="253" height="416" alt="image" src="https://github.com/user-attachments/assets/a349c209-844a-4457-8388-c488e90cc50a" />  

・Avatar Self Scaling(SpoutCamera)  
VirtualMotionCaptureはキャリブレーション後に身長に合わせるためにカメラの位置を実際の位置よりアバターに近づきます。  
そのためBeatSaberでカメラに寄るとカメラ位置より手前でアバターが消失するため、
チェックを入れるとカメラ位置を本来の位置（SpoutCameraのみ）に戻し、アバターそのもののサイズを調整します。
  
・Display Dummy Floor  
VirtualMotionCapture内に床を表示します。SpoutCameraには映りません。
  
・Height Adjust Offset  
アバターロード時に靴やヒールを探して床面を自動で調整しますが、
調整が必要な場合に手動で調整してください。
こちらもキャリブレーション前に調整してください。
  
・Blendshape Sync Mesh  
チェックを入れたVRMアバターにあるMeshのシェイプキーを、ロードしたVRCアバターで同期します。
基本的には顔メッシュ（Body）にチェックをいれて使用します。
