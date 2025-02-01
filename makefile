# Variables
SOLUTION_FILE = Softwrox.Forensics.Concordance.sln
PROJECT_DIR = Softwrox.Forensics.Concordance.Cli/             # Replace with your project directory name
TEST_PROJECT = Softwrox.Forensics.Concordance.Core.Test/     # Replace with your test project directory name

# Default target
all: build

# Build the solution
build:
	dotnet build $(SOLUTION_FILE)

# Run the solution
run: build
	dotnet run --project $(PROJECT_DIR) -- --file ./SavedSearch_2168833_export.dat

# Test the solution
test:
	dotnet test $(TEST_PROJECT)

# Clean the build output
clean:
	dotnet clean $(SOLUTION_FILE)

# Restore dependencies
restore:
	dotnet restore $(SOLUTION_FILE)

# Format code (optional, for consistency)
format:
	dotnet format $(SOLUTION_FILE)

# Help target
help:
	@echo "Available targets:"
	@echo "  build    - Build the solution"
	@echo "  run      - Run the application"
	@echo "  test     - Run all tests"
	@echo "  clean    - Clean the build output"
	@echo "  restore  - Restore dependencies"
	@echo "  format   - Format the code"
	@echo "  help     - Show this help message"
