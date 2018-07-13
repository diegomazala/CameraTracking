// Fill out your copyright notice in the Description page of Project Settings.

#include "StypeListener.h"
#include "Misc/Base64.h"
#include "Kismet/GameplayStatics.h"

#include "Camera/CameraComponent.h"
#include "Materials/MaterialInstanceDynamic.h"


// Sets default values
AStypeListener::AStypeListener()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

}

// Called when the game starts or when spawned
void AStypeListener::BeginPlay()
{
	Super::BeginPlay();
	
	//
	// Setup Socket Listener
	//
	FIPv4Endpoint Endpoint(FIPv4Address::Any, 6301);
	//
	ListenSocket = FUdpSocketBuilder(TEXT("StypeSocket"))
		.AsNonBlocking()
		.AsReusable()
		.BoundToEndpoint(Endpoint)
		.WithReceiveBufferSize(256);
	//
	int32 SendSize = 256;
	ListenSocket->SetReceiveBufferSize(SendSize, SendSize);
	//
	FTimespan ThreadWaitTime = FTimespan::FromMilliseconds(3);
	SocketReceiver = new FUdpSocketReceiver(ListenSocket, ThreadWaitTime, TEXT("UDP_SOCKET_RECEIVER"));
	SocketReceiver->OnDataReceived().BindUObject(this, &AStypeListener::OnReceiveData);
	SocketReceiver->Start();



	//
	// Create Lens Distortion Material and Apply to Camera
	//
	LensDistortion = UMaterialInstanceDynamic::Create(LensDistortionParentMaterial, this, FName(TEXT("Lens Distortion Material Dynamic")));
	LensDistortion->SetFlags(RF_Transient);
	//
	if (CameraActor)
	{
		UActorComponent* actorCam = CameraActor->GetComponentByClass(UCameraComponent::StaticClass());
		this->CameraComponent = dynamic_cast<UCameraComponent*>(actorCam);
		CameraComponent->AddOrUpdateBlendable(LensDistortion);

		auto PlayerController = UGameplayStatics::GetPlayerController(GetWorld(), 0);
		if (PlayerController)
			PlayerController->SetViewTargetWithBlend(CameraActor, 2.f);
	}
}


void AStypeListener::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
	Super::EndPlay(EndPlayReason);
	//~~~~~~~~~~~~~~~~

	// Clear all sockets!
	if (SocketReceiver)
	{
		SocketReceiver->Stop();
		delete SocketReceiver;
		SocketReceiver = nullptr;
	}
	//
	// makes sure repeat plays in Editor dont hold on to old sockets!
	if (ListenSocket)
	{
		ListenSocket->Close();
		ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->DestroySocket(ListenSocket);
	}

}



void AStypeListener::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	const auto& Packet = PacketBuffer.get();
	
	if (CameraActor)
	{
		CameraActor->SetActorLocation(FVector(Packet.x(), Packet.z(), Packet.y()) * 100.0f);
		CameraActor->SetActorRotation(FRotator(Packet.tilt(), Packet.pan(), Packet.roll()));

		if (CameraComponent)
		{
			CameraComponent->SetAspectRatio(Packet.aspect_ratio());
			CameraComponent->SetFieldOfView(Packet.fovx());
		}

		if (LensDistortion)
		{
			LensDistortion->SetScalarParameterValue("K1", Packet.k1());
			LensDistortion->SetScalarParameterValue("K2", Packet.k2());
			LensDistortion->SetScalarParameterValue("ChipWidth", Packet.chip_width());
			LensDistortion->SetScalarParameterValue("ChipHeight", Packet.chip_width() / Packet.aspect_ratio());
			LensDistortion->SetScalarParameterValue("CenterX", Packet.center_x());
			LensDistortion->SetScalarParameterValue("CenterY", Packet.center_y());
		}
	}


#if 0 /////// for debug purposes
	//
	// If SocketReceiver was not initialized, call receive manually
	//
	if (!SocketReceiver)
		DoReceiveData();


	//
	// Print Packet
	//

	UE_LOG(LogTemp, Log, TEXT("%d %lu %f %f %f %f %f %f %f %f %f %f %f %f %f %f %f"),
		Packet.package_number(), Packet.timecode(),
		Packet.x(), Packet.y(), Packet.z(),
		Packet.pan(), Packet.tilt(), Packet.roll(),
		Packet.fovx(), Packet.aspect_ratio(),
		Packet.focus(), Packet.zoom(),
		Packet.k1(), Packet.k2(),
		Packet.center_x(), Packet.center_y(),
		Packet.chip_width()
	);

	UE_LOG(LogTemp, Log, TEXT("%d %d %d %d %d"),
		PacketBuffer[0].package_number(),
		PacketBuffer[1].package_number(),
		PacketBuffer[2].package_number(),
		PacketBuffer[3].package_number(),
		PacketBuffer[4].package_number());
#endif


}


void AStypeListener::OnReceiveData(const FArrayReaderPtr& data, const FIPv4Endpoint&)
{
	if (data->TotalSize() == buffer_index::total)
	{
		StypeHFPacket recv_packet(data->GetData());

		if (recv_packet.IsValid())
		{
			PacketBuffer.put(recv_packet);
		}
		else
		{
			UE_LOG(LogTemp, Warning, TEXT("Packet is Invalid"));
		}
	}
	else
	{
		UE_LOG(LogTemp, Warning, TEXT("Packet size does not match"));
	}
}

//
// Receive the data when this method is called
//
// Note: Do not use FUdpSocketReceiver* SocketReceiver when using this funcition
//
void AStypeListener::DoReceiveData()
{

	TSharedRef<FInternetAddr> targetAddr = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
	TArray<uint8> ReceivedData;

	uint32 Size;
	while (ListenSocket->HasPendingData(Size))
	{
		uint8 *Recv = new uint8[Size];
		int32 BytesRead = 0;

		ReceivedData.SetNumUninitialized(FMath::Min(Size, 65507u));

		ListenSocket->RecvFrom(ReceivedData.GetData(), ReceivedData.Num(), BytesRead, *targetAddr);

		if (BytesRead == buffer_index::total)
		{
			StypeHFPacket recv_packet(ReceivedData.GetData());
			if (recv_packet.IsValid())
				PacketBuffer.put(recv_packet);
			else
				UE_LOG(LogTemp, Warning, TEXT("Packet is Invalid"));
		}
		else
		{
			UE_LOG(LogTemp, Warning, TEXT("Packet size does not match"));
		}
	}

}
