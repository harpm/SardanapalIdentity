package main

import (
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
)

type BuildInfo struct {
	Version        string
	Projects_Path  []string
	Nuget_Provider string
}

type LogLevel int

const (
	Info_Level LogLevel = iota
	Debug_Level
	Warning_Level
	Error_Level
)

func Log(content string, level LogLevel) {

	if level == Debug_Level {
		fmt.Printf("\nLog [Debug]: %s\n", content)
	} else if level == Info_Level {
		fmt.Printf("\nLog [Info]: %s\n", content)
	} else if level == Warning_Level {
		fmt.Printf("\nLog [Warning]: %s\n", content)
	} else if level == Error_Level {
		fmt.Printf("\nLog [Error]: %s\n", content)
	}
}

func main() {

	var data BuildInfo
	b_data, _ := os.ReadFile("Build.json")

	err := json.Unmarshal(b_data, &data)
	if err != nil {
		panic(err)
	}

	Log(fmt.Sprintf("Projects Path: \n%s", data.Projects_Path[:]), Info_Level)

	for i := 0; i < len(data.Projects_Path); i++ {
		Log(fmt.Sprintf("-------------------- Started pipeline for project: %s --------------------", data.Projects_Path[i]), Info_Level)

		Log(fmt.Sprintf("Cleaning project...\n\tPath: %s", data.Projects_Path[i]), Info_Level)

		clean_cmd := exec.Command("dotnet",
			"clean",
			data.Projects_Path[i])

		output, err := clean_cmd.Output()

		if err != nil {
			Log(fmt.Sprintf("Failed project %s\nResult:%s\nError: %s", data.Projects_Path[i], output, err), Error_Level)
			panic();
		}

		fmt.Printf("Output: \t%s", string(output[:]))

		Log(fmt.Sprintf("Restoring project...\n\tPath: %s", data.Projects_Path[i]), Info_Level)

		restore_cmd := exec.Command("dotnet",
			"restore",
			data.Projects_Path[i])

		output, err = restore_cmd.Output()

		if err != nil {
			Log(fmt.Sprintf("Failed project %s\nResult:%s\nError: %s", data.Projects_Path[i], output, err), Error_Level)
			panic();
		}

		fmt.Printf("Output: \t%s", string(output[:]))

		Log(fmt.Sprintf("Building project...\n\tPath: %s", data.Projects_Path[i]), Info_Level)

		build_cmd := exec.Command("dotnet",
			"build",
			data.Projects_Path[i],
			"--no-restore",
			"--configuration",
			"release",
			fmt.Sprintf("-p:Version=%s", data.Version))

		output, err = build_cmd.Output()

		if err != nil {
			Log(fmt.Sprintf("Failed project %s\nResult:%s\nError: %s", data.Projects_Path[i], output, err), Error_Level)
			panic();
		}

		// log output message
		fmt.Printf("Output: \t%s", string(output[:]))

		Log(fmt.Sprintf("-------------------- Ended pipeline for project: %s --------------------", data.Projects_Path[i]), Info_Level)
	}

	for i := 0; i < len(data.Projects_Path); i++ {
		Log(fmt.Sprintf("Publishing project artifacts...\n\tPath: %s", data.Projects_Path[i]), Info_Level)

		publish_cmd := exec.Command("dotnet",
			"nuget",
			"push",
			fmt.Sprintf("/home/runner/work/SardanapalIdentity/SardanapalIdentity/%s/bin/release/%s.%s.nupkg", data.Projects_Path[i], data.Projects_Path[i], data.Version),
			"-s",
			data.Nuget_Provider,
			"--skip-duplicate")

		output, err := publish_cmd.Output()

		if err != nil {
			Log(fmt.Sprintf("Failed project %s\nResult:%s\nError: %s", data.Projects_Path[i], output, err), Error_Level)
			panic()
		}

		fmt.Printf("Output: \t%s", string(output[:]))
	}

	if err != nil {
		panic(err)
	}
}
