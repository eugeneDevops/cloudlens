.PHONY: test test-integration eval up down logs format

test:
	dotnet test --filter "FullyQualifiedName~UnitTests"

test-integration:
	dotnet test --filter "FullyQualifiedName~IntegrationTests"

eval:
	dotnet test --filter "FullyQualifiedName~EvalTests"

format:
	dotnet format

up:
	kind create cluster --name cloudlens --config deploy/k8s/kind.yaml || true
	kubectl apply -k deploy/k8s/overlays/local
	kubectl wait --for=condition=available --timeout=300s deployment --all

down:
	kind delete cluster --name cloudlens

logs:
	kubectl logs -l app.kubernetes.io/part-of=cloudlens --tail=100 -f
