// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "Networking.h"
#include "Runtime/Networking/Public/Interfaces/IPv4/IPv4Address.h"

#include "StypeHFPacket.h"

#include "StypeListener.generated.h"

class UCameraComponent;
class UMaterial;
class UMaterialInstanceDynamic;

UCLASS()
class STYPE_API AStypeListener : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	AStypeListener();
	
	virtual void Tick(float DeltaTime) override;

	
protected:

	virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;
		
	void OnReceiveData(const FArrayReaderPtr& data, const FIPv4Endpoint&);
	void DoReceiveData();

	FSocket*			ListenSocket;
	FUdpSocketReceiver* SocketReceiver;

	//UPROPERTY(EditAnywhere)
	FIPv4Address LocalIpAddress;

	//UPROPERTY(EditAnywhere)
	FIPv4Address RemoteIpAddress;

	UPROPERTY(EditAnywhere)
	uint32 Port = 6301;

	UPROPERTY(EditAnywhere)
	AActor* CameraActor = nullptr;

	UPROPERTY(VisibleAnywhere)
	UCameraComponent* CameraComponent;

	UPROPERTY(EditAnywhere)
	UMaterial* LensDistortionParentMaterial = nullptr;

	UPROPERTY(VisibleAnywhere)
	UMaterialInstanceDynamic* LensDistortion = nullptr;

	StypeHFPacket Packet;
};
