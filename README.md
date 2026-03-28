# VMC Replace Avatar
VRCアバターから変換して出力したアバターデータを読み込み、VRMと置き換えるVMCMod  
BeatSaber用アバターとして使用することを想定しています。
  
## 必要なもの
・VirtualMotion Capture  
[https://vmc.info/](https://vmc.info/)  
  
・AvatarExporter(アバターデータ出力用)  
[PhysicsConverter](https://github.com/Snow1226/PhysicsConverter)

・VMC Sport（VMCMod）  
[https://github.com/Snow1226/VMCSpout](https://github.com/Snow1226/VMCSpout)  

## 使い方  
Avatar Changeボタンを押して、事前にVRC用Projectから出力したアバターをロードしてください。  
※現状キャリブレーション前に呼び出さないと位置が正常になりません。  
  
## 設定
<img width="248" height="697" alt="image" src="https://github.com/user-attachments/assets/7d42cfab-f2b8-4a0f-b55c-9d8884866593" />

### Avatar Self Scaling  
VirtualMotionCaptureはキャリブレーション後に身長に合わせるために、  
カメラの位置が実際の位置よりアバターに近づきます。  
そのためBeatSaberでカメラに寄るとカメラ位置より手前でアバターが消失するため、
チェックを入れるとカメラ位置を本来の位置（SpoutCameraのみ）に戻し、アバターそのもののサイズを調整します。

### AvatarChange  
[Physics Converter](https://github.com/Snow1226/PhysicsConverter)で出力したアバターを読み込みます。  
  
### Display Dummy Floor  
VirtualMotionCapture内に床を表示します。SpoutCameraには映りません。
  
### Height Adjust Offset  
アバターロード時に靴やヒールを探して床面を自動で調整しますが、
調整が必要な場合に手動で調整してください。
こちらもキャリブレーション前に調整してください。

### Select footwear  
床面調整のため、靴やヒール等のメッシュを選択し、Adjustボタンを押すことで床面に合わせます。  
ロード時に自動で実行しますが、衣装によっては靴より下になるメッシュがある場合があるため、  
その場合には手動で靴を選択し、Adjustを押してください。  
  
### Blendshape Sync Mesh  
チェックを入れたVRMアバターにあるMeshのシェイプキーを、ロードしたVRCアバターで同期します。
基本的には顔メッシュ（Body）にチェックをいれて使用します。
  
### Floor Offset Sender  
BeatSaberに床面調整用の命令を送信するポートを指定します。  
  
### Display UI at Startup  
VirtualMotionCapture起動時にこのUIを表示するかどうかを選択します。  
非表示にした後は”TABキー”か”詳細設定のロード済みModリスト”から再度表示させることができます。  
