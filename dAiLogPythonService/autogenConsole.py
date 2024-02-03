import autogen
import logging
from autogen import AssistantAgent, GroupChat, GroupChatManager, UserProxyAgent
from autogen.agentchat.contrib.gpt_assistant_agent import GPTAssistantAgent
import autogen
import os

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


current_dir = os.path.dirname(os.path.abspath(__file__))
print(current_dir)
config_list_path = os.path.join(current_dir, "OAI_CONFIG_LIST.json")
logger.info("Using config list path: %s", config_list_path)

config_list_gpt4 = autogen.config_list_from_json(
    config_list_path,
    filter_dict={"model": ["gpt-4-turbo-preview"]},
)
llm_config = {"config_list": config_list_gpt4}

user_proxy = UserProxyAgent(
    name="User_proxy",
    system_message="A human admin.",
    code_execution_config={
        "last_n_messages": 2,
        "work_dir": "groupChat",
        "use_docker": False,  # Set to False as Docker is not being used
    },
    human_input_mode="ALWAYS",
    is_termination_msg=lambda x: x.get("content", "").rstrip().endswith("TERMINATE"),
)
# Define GPTAssistantAgents with similar configuration for code execution
coder = GPTAssistantAgent(
    name="Coder",
    llm_config=llm_config,
    instructions=AssistantAgent.DEFAULT_SYSTEM_MESSAGE,
    code_execution_config={"use_docker": False}
)
analyst = GPTAssistantAgent(
    name="Data_analyst",
    instructions="You are a data analyst that offers insight into data.",
    llm_config=llm_config,
    code_execution_config={"use_docker": False})
engineer = autogen.AssistantAgent(
    name="Engineer",
    llm_config={"config_list": config_list_gpt4},
    system_message="""Engineer. You follow an approved plan. You write python/shell code to solve tasks. Wrap the 
    code in a code block that specifies the script type. The user can't modify your code. So do not suggest 
    incomplete code which requires others to modify. Don't use a code block if it's not intended to be executed by 
    the executor.Don't include multiple code blocks in one response. Do not ask others to copy and paste the result. 
    Check the execution result returned by the executor.If the result indicates there is an error, fix the error and 
    output the code again. Suggest the full code instead of partial code or code changes. If the error can't be fixed 
    or if the task is not solved even after the code is executed successfully, analyze the problem, revisit your 
    assumption, collect additional info you need, and think of a different approach to try.""",
    code_execution_config={"use_docker": False})

scientist = autogen.AssistantAgent(
    name="Scientist",
    llm_config={"config_list": config_list_gpt4},
    system_message="""Scientist. You follow an approved plan. You are able to categorize papers after seeing their 
    abstracts printed. You don't write code.""",
    code_execution_config={"use_docker": False})

planner = autogen.AssistantAgent(
    name="Planner",
    system_message="""Planner. Suggest a plan. Revise the plan based on feedback from admin and critic, until admin 
    approval.The plan may involve an engineer who can write code and a scientist who doesn't write code.Explain the 
    plan first. Be clear which step is performed by an engineer, and which step is performed by a scientist.""",
    llm_config={"config_list": config_list_gpt4},
    code_execution_config={"use_docker": False})

critic = autogen.AssistantAgent(
    name="Critic",
    system_message="Critic. Double check plan, claims, code from other agents and provide feedback. Check whether the "
                   "plan includes adding verifiable info such as source URL.",
    llm_config={"config_list": config_list_gpt4},
    code_execution_config={"use_docker": False}
)

executor = autogen.UserProxyAgent(
    name="Executor",
    system_message="Executor. Execute the code written by the engineer and report the result.",
    human_input_mode="NEVER",
    code_execution_config={
        "last_n_messages": 3,
        "work_dir": "consoleApp",
        "use_docker": False,
    },
)

group_chat = GroupChat(
    agents=[user_proxy, coder, analyst, engineer, scientist, planner, critic,executor],  # Other agents
    messages=[],
    max_round=10
)
manager = GroupChatManager(groupchat=group_chat, llm_config=llm_config, code_execution_config={"use_docker": False})

prompt = """Develop a sophisticated Python trading bot capable of securely logging 
into a specified stock trading service, utilizing advanced AI algorithms to conduct a comprehensive analysis of stock 
market trends and individual stock performances. This bot should be equipped to handle high-frequency trading, operating
 on various time scales including daily, hourly, and down to the second, to execute well-timed buy and sell orders. 
 It should have the capability to dynamically adjust its trading strategies based on real-time market data, predict 
 future market movements with high accuracy, and automatically optimize its algorithms to continuously improve 
 its decision-making processes for maximizing profits. Additionally, ensure the bot includes robust risk management 
 features, adheres to pre-set trading limits to minimize potential losses, and generates detailed performance 
 reports for user review."""


user_proxy.initiate_chat(manager, message=prompt)



