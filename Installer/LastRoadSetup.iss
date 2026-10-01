#define MyAppName "Last Road"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Last Road"
#define MyAppURL "https://last-road.com/"
#define MyAppExeName "LastRoadLauncher.exe"

[Setup]
AppId={{D53A34F7-6E9B-4D61-A51A-6E7B8A73C1F4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName=Last Road {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={sd}\Games\Last Road
DefaultGroupName=Last Road
DisableProgramGroupPage=yes
DisableReadyPage=yes

OutputDir=Output
OutputBaseFilename=LastRoadSetup

Compression=lzma2
SolidCompression=yes

WizardStyle=modern dark windows11 hidebevels
WizardImageFile=
WizardSmallImageFile=
WizardBackColor=#090B0C

SetupIconFile=launcher-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

CloseApplications=yes
RestartApplications=no

ShowLanguageDialog=no
LanguageDetectionMethod=none
UsePreviousLanguage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{app}"; Permissions: users-modify

[Files]
Source: "..\LastRoadLauncher\Assets\launcher-background.png"; DestName: "last-road-background.png"; Flags: dontcopy
Source: "LastRoadLauncher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "launcher-icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Last Road"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\launcher-icon.ico"
Name: "{autodesktop}\Last Road"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\launcher-icon.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Last Road"; Flags: nowait postinstall skipifsilent


[Code]

var
  RoadArt: TBitmap;
  RoadTitle: TNewStaticText;
  RoadTagline: TNewStaticText;


function SetStretchBltMode(
  DC: THandle;
  Mode: Integer
): Integer;
  external 'SetStretchBltMode@gdi32.dll stdcall';


function StretchBlt(
  DestDC: THandle;
  X, Y, W, H: Integer;
  SourceDC: THandle;
  SX, SY, SW, SH: Integer;
  Rop: LongWord
): Boolean;
  external 'StretchBlt@gdi32.dll stdcall';


function AlphaBlend(
  DestDC: THandle;
  XOriginDest, YOriginDest, WidthDest, HeightDest: Integer;
  SrcDC: THandle;
  XOriginSrc, YOriginSrc, WidthSrc, HeightSrc: Integer;
  BlendFunction: LongWord
): Boolean;
  external 'AlphaBlend@msimg32.dll stdcall';


procedure DrawContentShade(
  Background: TBitmap;
  X, Y, W, H: Integer
);
var
  Shade: TBitmap;
  BlendFunction: LongWord;
begin
  Shade := TBitmap.Create;

  try
    Shade.Width := W;
    Shade.Height := H;

    Shade.Canvas.Brush.Color := $00201D1B;
    Shade.Canvas.Pen.Color := $00201D1B;

    Shade.Canvas.Rectangle(
      0,
      0,
      W + 1,
      H + 1
    );

    { Approx. 71% opacity }
    BlendFunction := $00B50000;

    AlphaBlend(
      Background.Canvas.Handle,
      X,
      Y,
      W,
      H,
      Shade.Canvas.Handle,
      0,
      0,
      W,
      H,
      BlendFunction
    );

  finally
    Shade.Free;
  end;
end;


procedure UpdateRoadBackground(Sender: TObject);
var
  Background: TBitmap;
  Images: TArrayOfGraphic;
  W, H: Integer;
  DrawW, DrawH: Integer;
begin
  if RoadArt = nil then
    Exit;

  W := WizardForm.ClientWidth;
  H := WizardForm.ClientHeight;

  if (W <= 0) or (H <= 0) then
    Exit;

  DrawW := W;
  DrawH := MulDiv(RoadArt.Height, W, RoadArt.Width);

  if DrawH < H then
  begin
    DrawH := H;
    DrawW := MulDiv(RoadArt.Width, H, RoadArt.Height);
  end;

  Background := TBitmap.Create;

  try
    Background.Width := W;
    Background.Height := H;

    SetStretchBltMode(
      Background.Canvas.Handle,
      4
    );

    StretchBlt(
      Background.Canvas.Handle,
      (W - DrawW) div 2,
      (H - DrawH) div 2,
      DrawW,
      DrawH,
      RoadArt.Canvas.Handle,
      0,
      0,
      RoadArt.Width,
      RoadArt.Height,
      $00CC0020
    );

    DrawContentShade(
      Background,
      ScaleX(24),
      ScaleY(82),
      W - ScaleX(48),
      ScaleY(270)
    );

    SetLength(Images, 1);
    Images[0] := Background;

    WizardSetBackImage(
      Images,
      False,
      True,
      105
    );

  finally
    Background.Free;
  end;
end;


procedure StyleRoadHeading(
  LabelControl: TNewStaticText;
  FontSize: Integer
);
begin
  LabelControl.StyleElements :=
    LabelControl.StyleElements - [seFont];

  LabelControl.Font.Name := 'Georgia';
  LabelControl.Font.Size := FontSize;
  LabelControl.Font.Style := [fsBold];
  LabelControl.Font.Color := $006BB5D6;
end;


procedure StyleBodyText(
  LabelControl: TNewStaticText
);
begin
  LabelControl.StyleElements :=
    LabelControl.StyleElements - [seFont];

  LabelControl.Font.Name := 'Segoe UI';
  LabelControl.Font.Size := 10;
  LabelControl.Font.Style := [fsBold];
  LabelControl.Font.Color := $00FFFFFF;
end;


procedure InitializeWizard;
var
  Png: TPngImage;
begin

  { WINDOW }

  WizardForm.ClientWidth := ScaleX(750);
  WizardForm.ClientHeight := ScaleY(450);
  WizardForm.Position := poScreenCenter;


  { BACKGROUND }

  ExtractTemporaryFile('last-road-background.png');

  Png := TPngImage.Create;
  RoadArt := TBitmap.Create;

  try
    Png.LoadFromFile(
      ExpandConstant('{tmp}\last-road-background.png')
    );

    RoadArt.Width := Png.Width;
    RoadArt.Height := Png.Height;
    RoadArt.Assign(Png);

  finally
    Png.Free;
  end;


  { HIDE STANDARD ARTWORK }

  WizardForm.WizardBitmapImage.Visible := False;
  WizardForm.WizardBitmapImage2.Visible := False;
  WizardForm.WizardSmallBitmapImage.Visible := False;

  WizardForm.Bevel.Visible := False;
  WizardForm.Bevel1.Visible := False;


  { MAIN LAYOUT }

  WizardForm.OuterNotebook.SetBounds(
    0,
    0,
    ScaleX(750),
    ScaleY(386)
  );

  WizardForm.MainPanel.SetBounds(
    0,
    0,
    ScaleX(750),
    ScaleY(140)
  );

  WizardForm.InnerNotebook.SetBounds(
    ScaleX(36),
    ScaleY(150),
    ScaleX(678),
    ScaleY(222)
  );


  { PAGE HEADING }

  WizardForm.PageNameLabel.SetBounds(
    ScaleX(36),
    ScaleY(88),
    ScaleX(678),
    ScaleY(30)
  );

  WizardForm.PageDescriptionLabel.SetBounds(
    ScaleX(36),
    ScaleY(120),
    ScaleX(678),
    ScaleY(28)
  );

  StyleRoadHeading(
    WizardForm.PageNameLabel,
    14
  );

  StyleBodyText(
    WizardForm.PageDescriptionLabel
  );


  { LAST ROAD }

  RoadTitle := TNewStaticText.Create(WizardForm);
  RoadTitle.Parent := WizardForm;
  RoadTitle.Caption := 'LAST ROAD';

  StyleRoadHeading(
    RoadTitle,
    24
  );

  RoadTitle.SetBounds(
    ScaleX(36),
    ScaleY(13),
    ScaleX(450),
    ScaleY(39)
  );

  RoadTitle.BringToFront;


  { TAGLINE }

  RoadTagline := TNewStaticText.Create(WizardForm);
  RoadTagline.Parent := WizardForm;

  RoadTagline.Caption :=
    'LINEAGE II · INTERLUDE  /  OLD WORLD. NEW PATH.';

  RoadTagline.StyleElements :=
    RoadTagline.StyleElements - [seFont];

  RoadTagline.Font.Name := 'Segoe UI';
  RoadTagline.Font.Size := 9;
  RoadTagline.Font.Style := [fsBold];
  RoadTagline.Font.Color := $00E8E8E8;

  RoadTagline.SetBounds(
    ScaleX(38),
    ScaleY(56),
    ScaleX(650),
    ScaleY(20)
  );

  RoadTagline.BringToFront;


  { WELCOME PAGE }

  WizardForm.WelcomeLabel1.SetBounds(
    ScaleX(36),
    ScaleY(110),
    ScaleX(678),
    ScaleY(66)
  );

  WizardForm.WelcomeLabel2.SetBounds(
    ScaleX(36),
    ScaleY(190),
    ScaleX(678),
    ScaleY(176)
  );

  StyleRoadHeading(
    WizardForm.WelcomeLabel1,
    18
  );

  StyleBodyText(
    WizardForm.WelcomeLabel2
  );


  { FINISH PAGE }

  WizardForm.FinishedHeadingLabel.SetBounds(
    ScaleX(36),
    ScaleY(110),
    ScaleX(678),
    ScaleY(66)
  );

  WizardForm.FinishedLabel.SetBounds(
    ScaleX(36),
    ScaleY(184),
    ScaleX(678),
    ScaleY(72)
  );

  WizardForm.RunList.SetBounds(
    ScaleX(36),
    ScaleY(264),
    ScaleX(678),
    ScaleY(68)
  );

  StyleRoadHeading(
    WizardForm.FinishedHeadingLabel,
    18
  );

  StyleBodyText(
    WizardForm.FinishedLabel
  );


  { BUTTONS }

  WizardForm.BackButton.SetBounds(
    ScaleX(348),
    ScaleY(400),
    ScaleX(112),
    ScaleY(34)
  );

  WizardForm.NextButton.SetBounds(
    ScaleX(470),
    ScaleY(400),
    ScaleX(122),
    ScaleY(34)
  );

  WizardForm.CancelButton.SetBounds(
    ScaleX(602),
    ScaleY(400),
    ScaleX(112),
    ScaleY(34)
  );

  WizardForm.BackButton.Font.Name := 'Segoe UI';
  WizardForm.BackButton.Font.Size := 10;

  WizardForm.NextButton.Font.Name := 'Segoe UI';
  WizardForm.NextButton.Font.Size := 10;
  WizardForm.NextButton.Font.Style := [fsBold];

  WizardForm.CancelButton.Font.Name := 'Segoe UI';
  WizardForm.CancelButton.Font.Size := 10;


  { DRAW }

  WizardForm.OnResize := @UpdateRoadBackground;
  UpdateRoadBackground(nil);

end;


procedure DeinitializeSetup;
begin
  if RoadArt <> nil then
  begin
    RoadArt.Free;
    RoadArt := nil;
  end;
end;

