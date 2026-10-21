# Linux container for Sharper (default; works with Docker on Linux, macOS, and Windows in Linux-containers mode).
#   docker build -t sharper-artifact .
#   docker run --rm sharper-artifact                                                    (runs run_quick_test.ps1)
#   docker run --rm -v "$PWD/results:/sharper/results" sharper-artifact ./run_experiments.ps1
# For a Windows-containers host, use Dockerfile.windows instead.
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0@sha256:3a48280a9a7c6276c3eeaba1efa6a08820f18c84fb9e6274c187719ecfaf9fbc

# Microsoft.Z3 4.12.2 (NuGet) ships native binaries only for Windows and macOS, so install the
# matching official Z3 4.12.2 Linux build. libgomp1 is the OpenMP runtime that libz3.so loads.
ARG Z3_VERSION=4.12.2
ARG Z3_SHA256=a198851a7403d8b25bab920bd4a4792efe1af5c28a5a932d840af0472d3a83eb
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 unzip \
    && rm -rf /var/lib/apt/lists/* \
    && curl -fsSL -o /tmp/z3.zip "https://github.com/Z3Prover/z3/releases/download/z3-${Z3_VERSION}/z3-${Z3_VERSION}-x64-glibc-2.31.zip" \
    && echo "${Z3_SHA256}  /tmp/z3.zip" | sha256sum -c - \
    && unzip -j /tmp/z3.zip "z3-${Z3_VERSION}-x64-glibc-2.31/bin/libz3.so" -d /usr/local/lib \
    && rm /tmp/z3.zip \
    && ldconfig

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

WORKDIR /sharper
COPY . .

# Restore and build Sharper and the benchmark projects that the pipeline runs.
RUN dotnet restore Sharper.sln && dotnet build Sharper.sln --no-restore

ENTRYPOINT ["pwsh", "-NoProfile", "-File"]
CMD ["./run_quick_test.ps1"]
