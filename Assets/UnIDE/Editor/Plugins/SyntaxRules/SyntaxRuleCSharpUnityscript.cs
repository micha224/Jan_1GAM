using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Reflection;

using UIDE.SyntaxRules.ExpressionResolvers.CSharp;
using UIDE.SyntaxRules.ExpressionResolvers;
using UIDE.SyntaxRules;
using UIDE.CodeCompletion.Parsing;

//using UIDE.CodeCompletion.CSharp;
using UIDE.CodeCompletion;

namespace UIDE.SyntaxRules.Shared {
	[System.Serializable]
	public class SyntaxRuleCSharpUnityscript:SyntaxRule {
		private Thread parserThread;
		private Thread chainResolverThread;
		
		private Thread updateMultiLineFormattingThread;
		private bool wantsMultiLineFormattingUpdate = false;
		private bool useUnityscript = false;
		
		private bool useMultiThreadingParser = true;
		
		private bool wantsParserUpdate = false;
		private bool wantsChainResolverUpdate = false;
		
		private ChainResolver chainResolver;
		private bool isCreatingChainResolver = false;
		
		private ParserInterface _parserInterface;
		public ParserInterface parserInterface {
			get {
				if (_parserInterface == null) {
					_parserInterface = new ParserInterface();
				}
				return _parserInterface;
			}
		}
		
		public SyntaxRuleCSharpUnityscript() {
			//isDefault = true;
			fileTypes = new string[] {".cs",".js"};
		}
		
		public override void OnTextEditorUpdate() {
			if (wantsParserUpdate) {
				if (Reparse()) {
					wantsParserUpdate = false;
				}
			}
			if (wantsChainResolverUpdate) {
				if (UpdateChainResolver()) {
					wantsChainResolverUpdate = false;
				}
			}
			if (wantsMultiLineFormattingUpdate) {
				if (UpdateMultilineFormatting()) {
					//wantsMultiLineFormattingUpdate = false;
				} 
			}
			
		}
		
		public override void Start() {
			useUnityscript = editor.extension == ".js";
			Reparse();
			UpdateChainResolver();
		}
		
		public override void OnFocus() {
			Reparse();
			UpdateChainResolver();
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnSwitchToTab() {
			Reparse();
			UpdateChainResolver();
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnRebuildLines(UIDEDoc doc) {
			base.OnRebuildLines(doc);
			Reparse();
			UpdateChainResolver();
		}
		
		public override void OnPostBackspace() {
			Reparse();
			UpdateChainResolver();
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnChangedCursorPosition(Vector2 pos) {
			UpdateChainResolver();
		}
		
		public override void OnPostEnterText(string text) {
			if (text == "\r" || text == "\n") {
				OnNewLine();
			}
			Reparse();
			UpdateChainResolver();
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override string OnPreEnterText(string text) {
			if (text == "}") {
				OnCloseCurly();
			}
			return text;
		}
		
		public void OnCloseCurly() {
			if (editor.cursor.posY <= 0) {
				return;
			}
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			UIDELine previousLine = editor.doc.GetLastNoneWhitespaceOrCommentLine(editor.cursor.posY-1);
			if (previousLine == line) {
				return;
			}
			
			if (!line.IsLineWhitespace()) {
				return;
			}
			
			UIDEElement firstElement = previousLine.GetFirstNonWhitespaceElement();
			int previousLineStartPos = previousLine.GetElementStartPos(firstElement);
			int screenPos = previousLine.GetScreenPosition(previousLineStartPos);
			UIDEElement lastElement = previousLine.GetLastNonWhitespaceElement();
			
			int tabCount = screenPos/4;
			if (lastElement != null) {
				if (lastElement.tokenDef.HasType("LineEnd")) {
					tabCount -= 1;
				}
			}
			tabCount = Mathf.Max(tabCount,0);
			line.rawText = line.GetTrimmedWhitespaceText();
			for (int i = 0; i < tabCount; i++) {
				line.rawText = "\t"+line.rawText;
			}
			
			//line.rawText += startingText;
			line.RebuildElements();
			editor.cursor.posX = tabCount;
		}
		public void OnNewLine() {
			if (editor.cursor.posY <= 0) {
				return;
			}
			
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			UIDELine previousLine = editor.doc.GetLastNoneWhitespaceOrCommentLine(editor.cursor.posY-1);
			if (previousLine == null) return;
			UIDEElement firstElement = previousLine.GetFirstNonWhitespaceElement();
			int previousLineStartPos = previousLine.GetElementStartPos(firstElement);
			int screenPos = previousLine.GetScreenPosition(previousLineStartPos);
			UIDEElement lastElement = previousLine.GetLastNonWhitespaceElement();
			
			string originalText = line.rawText;
			int tabCount = screenPos/4;
			if (lastElement != null && lastElement.rawText == "{") {
				tabCount += 1;
			}
			line.rawText = line.GetTrimmedWhitespaceText();
			for (int i = 0; i < tabCount; i++) {
				line.rawText = "\t"+line.rawText;
			}
			line.RebuildElements();
			
			Vector2 oldCursorPos = editor.cursor.GetVectorPosition();
			editor.cursor.posX = tabCount;
			Vector2 newCursorPos = editor.cursor.GetVectorPosition();
			//add another undo with the same name as the previous one so it gets grouped.
			if (editor.undoManager.undos.Count > 0) {
				string undoName = editor.undoManager.undos[editor.undoManager.undos.Count-1].groupID;
				editor.undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
			}
		}
		
		public override bool CheckIfStringIsKeyword(string str) {
			if (useUnityscript) {
				return !(UIDE.SyntaxRules.Unityscript.Keywords.keywordHash.Get(str) == null);
			}
			return !(UIDE.SyntaxRules.CSharp.Keywords.keywordHash.Get(str) == null);
		}
		public override bool CheckIfStringIsModifier(string str) {
			if (useUnityscript) {
					return !(UIDE.SyntaxRules.Unityscript.Keywords.modifierHash.Get(str) == null);
			}
			return !(UIDE.SyntaxRules.CSharp.Keywords.modifierHash.Get(str) == null);
		}
		public override bool CheckIfStringIsPrimitiveType(string str) {
			if (useUnityscript) {
					return !(UIDE.SyntaxRules.Unityscript.Keywords.primitiveTypeHash.Get(str) == null);
			}
			return !(UIDE.SyntaxRules.CSharp.Keywords.primitiveTypeHash.Get(str) == null);
		}
		public override UIDETokenDef GetKeywordTokenDef(UIDETokenDef tokenDef, string str) {
			if (CheckIfStringIsKeyword(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,Keyword");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			else if (CheckIfStringIsModifier(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,Modifier");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			else if (CheckIfStringIsPrimitiveType(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,PrimitiveType");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			if (APITokens.IsTypeKeyword(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,APIToken,Type");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			return tokenDef;
		}
		
		public override string ResolveExpressionAt(Vector2 position, int dir) {
			ExpressionResolver.editor = editor;
			return ExpressionResolver.ResolveExpressionAt(position,dir);
		}
		
		
		private bool Reparse() {
			if (useMultiThreadingParser) {
				if (parserThread != null && parserThread.IsAlive) {
					wantsParserUpdate = true;
					return false;
				}
				wantsParserUpdate = false;
				parserThread = new Thread(ReparseActual);
				parserThread.IsBackground = true;
				parserThread.Priority = System.Threading.ThreadPriority.BelowNormal;
				parserThread.Start();
				//Debug.Log("Reparsing...");
			}
			else {
				ReparseActual();
				wantsParserUpdate = false;
			}
			return true;
		}
		
		private void ReparseActual() {
			string text = editor.doc.GetParsableText();
			if (useUnityscript) {
				parserInterface.Reparse(this,text,"us");
			}
			else {
				parserInterface.Reparse(this,text,"cs");
			}
		}
		
		private void VerifyChainResolver() {
			if (chainResolver == null) {
				if (isCreatingChainResolver) {
					while (isCreatingChainResolver) {};
				}
				else {
					UpdateChainResolver();
				}
			}
		}
		
		private bool UpdateChainResolver() {
			if (useMultiThreadingParser) {
				if (chainResolverThread != null && chainResolverThread.IsAlive) {
					wantsChainResolverUpdate = true;
					return false;
				}
				wantsChainResolverUpdate = false;
				chainResolverThread = new Thread(UpdateChainResolverActual);
				chainResolverThread.IsBackground = true;
				chainResolverThread.Priority = System.Threading.ThreadPriority.BelowNormal;
				chainResolverThread.Start();
				//Debug.Log("Reparsing...");
			}
			else {
				UpdateChainResolverActual();
				wantsChainResolverUpdate = false;
			}
			return true;
		}
		private void UpdateChainResolverActual() {
			isCreatingChainResolver = true;
			try {
				chainResolver = new ChainResolver(editor,editor.cursor.GetVectorPosition());
			}
			catch (System.Exception ex) {
				Debug.LogError(ex.Message);
			}
			isCreatingChainResolver = false;
		}
		
		
		
		public override CompletionMethod[] GetMethodOverloads(Vector2 pos) {
			//Vector2 originalPos = pos;
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			pos = editor.doc.IncrementPosition(pos,-1);
			pos = editor.doc.IncrementPosition(pos,-1);
			pos = editor.doc.GoToEndOfWhitespace(pos,-1);
			
			
			//char nextChar = editor.doc.GetCharAt(pos);
			if (editor.doc.GetCharAt(pos) == '>') {
				ExpressionResolver.editor = editor;
				pos = ExpressionResolver.SimpleMoveToEndOfScope(pos,-1,ExpressionBracketType.Generic);
				pos = editor.doc.GoToEndOfWhitespace(pos,-1);
				if (useUnityscript) {
					if (editor.doc.GetCharAt(pos) == '.') {
						pos = editor.doc.IncrementPosition(pos,-1);
					}
				}
				pos = editor.doc.GoToEndOfWhitespace(pos,-1);
				//GameObject go;
				//go.GetComponent<Vector3>();
			}
			
			Vector2 endWordPos = pos;
			
			pos = editor.doc.GoToEndOfWord(pos,-1);
			Vector2 startWordPos = editor.doc.IncrementPosition(pos,1);
			pos = editor.doc.GoToEndOfWhitespace(pos,-1);
			//
			
			//Debug.Log(editor.doc.GetCharAt(pos));
			bool hasDot = false;
			if (editor.doc.GetCharAt(pos) == '.') {
				if (useUnityscript) {
					if (editor.doc.GetCharAt(editor.doc.IncrementPosition(pos,1)) != '<') {
						hasDot = true;
					}
				}
				else {
					hasDot = true;
				}
			}
			
			UIDELine startLine = editor.doc.RealLineAt((int)startWordPos.y);
			string functionName = startLine.rawText.Substring((int)startWordPos.x,((int)endWordPos.x-(int)startWordPos.x)+1);
			
			pos = editor.doc.IncrementPosition(pos,-1);
			pos = editor.doc.GoToEndOfWhitespace(pos,-1);
			
			string str = editor.syntaxRule.ResolveExpressionAt(pos,-1);
			if (useUnityscript) {
				str = str.Replace(".<","<");
			}
			//Debug.Log(str);
			
			CompletionMethod[] methods = new CompletionMethod[0];
			ChainResolver sigChainResolver = new ChainResolver(editor,pos);
			
			//Handle constructors
			bool isDirectConstructor = str == "new|";
			bool isIndirectConstructor = !isDirectConstructor && str.StartsWith("new|");
			if (isIndirectConstructor && hasDot) {
				isIndirectConstructor = false;
			}
			if (isIndirectConstructor) {
				ChainItem item = null;
				item = sigChainResolver.ResolveChain(str+"."+functionName);
				if (item == null || item.finalLinkType == null) {
					return methods;
				}
				methods = sigChainResolver.GetConstructors(item.finalLinkType);
				return methods;
			}
			else if (isDirectConstructor) {
				ChainItem item = null;
				item = sigChainResolver.ResolveChain(functionName);
				if (item == null || item.finalLinkType == null) {
					return methods;
				}
				methods = sigChainResolver.GetConstructors(item.finalLinkType);
				return methods;
			}
			
			System.Type type = sigChainResolver.reflectionDB.currentType;
			bool isStatic = false;
			if (hasDot) {
				ChainItem item = null;
				item = sigChainResolver.ResolveChain(str,false);
				if (item == null || item.finalLinkType == null) {
					return methods;
				}
				isStatic = item.finalLink.isStatic;
				type = item.finalLinkType;
			}
			
			methods = sigChainResolver.GetMethodOverloads(type,functionName,isStatic);
			
			return methods;
		}
		
		public override TooltipItem GetTooltipItem(Vector2 pos) {
			Vector2 originalPos = pos;
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			
			pos = editor.doc.GoToEndOfWord(pos,1);
			Vector2 endWordPos = editor.doc.IncrementPosition(pos,-1);
			pos = editor.doc.GoToEndOfWhitespace(pos,1);
			
			ExpressionInfo result = new ExpressionInfo();
			result.startPosition = pos;
			result.endPosition = pos;
			result.initialized = true;
			
			char nextChar = editor.doc.GetCharAt(pos);
			bool isBeginGeneric = nextChar == '<';
			if (useUnityscript) {
				Vector2 charAfterPos = editor.doc.GoToEndOfWhitespace(editor.doc.IncrementPosition(pos,1),1);
				char charAfter = editor.doc.GetCharAt(charAfterPos);
				if (nextChar == '.' && charAfter == '<') {
					pos = charAfterPos;
					nextChar = editor.doc.GetCharAt(pos);
					isBeginGeneric = true;
				}
			}
			//GameObject go;
			//go.GetComponent<Vector3>();
			if (isBeginGeneric) {
				result.startPosition = pos;
				result.endPosition = pos;
				ExpressionResolver.editor = editor;
				result = ExpressionResolver.CountToExpressionEnd(result,1,ExpressionBracketType.Generic);
				
				pos = result.endPosition;
				pos = editor.doc.IncrementPosition(pos,1);
				pos = editor.doc.GoToEndOfWhitespace(pos,1);
				
				result.startPosition = pos;
				result.endPosition = pos;
				nextChar = editor.doc.GetCharAt(pos);
			}
			
			bool isFunction = false;
			if (nextChar == '(') {
				ExpressionResolver.editor = editor;
				result = ExpressionResolver.CountToExpressionEnd(result,1,ExpressionBracketType.Expression);
				pos = result.endPosition;
				nextChar = editor.doc.GetCharAt(pos);
				isFunction = true;
			}
			
			if (!isFunction) {
				pos = endWordPos;
			}
			
			//Debug.Log(nextChar+" "+editor.doc.GetCharAt(endWordPos));
			
			string str = editor.syntaxRule.ResolveExpressionAt(pos,-1);
			if (useUnityscript) {
				str = str.Replace(".<","<");
			}
			//Debug.Log(str);
			
			ChainResolver sigChainResolver = new ChainResolver(editor,originalPos);
			ChainItem item = null;
			item = sigChainResolver.ResolveChain(str,false);
			
			TooltipItem tooltipItem = null;
			if (item != null) {
				if (item.finalLinkType != null) {
					tooltipItem = new TooltipItem(item.finalLinkType.Name+" "+item.finalLink.name);
					tooltipItem.clrType = item.finalLinkType;
				}
				
				if (item.finalLink.completionItem != null) {
					tooltipItem = new TooltipItem(item.finalLink.completionItem);
				}
				
			}
			
			return tooltipItem;
		}
		
		public override CompletionItem[] GetChainCompletionItems() {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			List<CompletionItem> items = new List<CompletionItem>();
			
			Vector2 previousCharPos = editor.cursor.GetVectorPosition();
			previousCharPos = editor.doc.IncrementPosition(previousCharPos,-1);
			
			UIDELine line = editor.doc.RealLineAt((int)previousCharPos.y);
			UIDEElement element = line.GetElementAt((int)previousCharPos.x);
			
			Vector2 expressionStartPos = previousCharPos;
			
			bool lastCharIsDot = element.tokenDef.HasType("Dot");
			if (lastCharIsDot) {
				expressionStartPos = editor.doc.IncrementPosition(expressionStartPos,-1);
			}
			else {
				int elementPos = line.GetElementStartPos(element);
				if (elementPos >= 2) {
					
					element = line.GetElementAt(elementPos-1);
					if (element.tokenDef.HasType("Dot")) {
						expressionStartPos.x = elementPos-2;
					}
					
				}
			}
			
			ChainItem item = null;
			string str = ResolveExpressionAt(expressionStartPos,-1);
			if (useUnityscript) {
				str = str.Replace(".<","<");
			}
			//Debug.Log(str);
			
			VerifyChainResolver();
			
			item = chainResolver.ResolveChain(str);
			
			if (item != null) {
				items = item.autoCompleteItems;
			}
			
			return items.ToArray();
		}
		
		public override CompletionItem[] GetGlobalCompletionItems() {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			//float startTime = Time.realtimeSinceStartup;
			List<CompletionItem> items = new List<CompletionItem>();
			
			//items = parserInterface.GetCurrentVisibleItems(editor.cursor.GetVectorPosition(), this).ToList();
			//Debug.Log(Time.realtimeSinceStartup-startTime);
			string[] keywords = UIDE.SyntaxRules.CSharp.Keywords.keywords;
			string[] modifiers = UIDE.SyntaxRules.CSharp.Keywords.modifiers;
			string[] primitiveTypes = UIDE.SyntaxRules.CSharp.Keywords.primitiveTypes;
			if (useUnityscript) {
				keywords = UIDE.SyntaxRules.Unityscript.Keywords.keywords;
				modifiers = UIDE.SyntaxRules.Unityscript.Keywords.modifiers;
				primitiveTypes = UIDE.SyntaxRules.Unityscript.Keywords.primitiveTypes;
			}
			for (int i = 0; i < keywords.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromKeyword(keywords[i]);
				items.Add(item);
			}
			for (int i = 0; i < modifiers.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromModifier(modifiers[i]);
				items.Add(item);
			}
			for (int i = 0; i < primitiveTypes.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromPrimitiveType(primitiveTypes[i]);
				items.Add(item);
			}
			
			//Add members of the current type
			string typeName = "new|"+GetCurrentTypeFullName(editor.cursor.GetVectorPosition())+"()";
			ChainItem typeItem = null;
			
			VerifyChainResolver();
			
			CompletionItem[] globalItems = chainResolver.GetCurrentlyVisibleGlobalItems();
			items.AddRange(globalItems);
			
			items.AddRange(parserInterface.GetCurrentVisibleItems(editor.cursor.GetVectorPosition(), this));
			
			typeItem = chainResolver.ResolveChain(typeName);
			if (typeItem != null) {
				items.AddRange(typeItem.autoCompleteItems);
			}
			
			string[] interfaces = GetCurrentTypeInterfaceNames(editor.cursor.GetVectorPosition());
			
			for (int i = 0; i < interfaces.Length; i++) {
				typeItem = null;
				typeItem = chainResolver.ResolveChain(interfaces[i]);
				if (typeItem != null) {
					items.AddRange(typeItem.autoCompleteItems);
				}
			}
			
			return items.ToArray();
		}
		
		public override string[] GetNamespacesVisibleInCurrentScope(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string[] namespaces = parserInterface.GetNamespacesVisibleInCurrentScope(pos,this);
			return namespaces;
		}
		public override string[] GetNamespaceChain(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string[] namespaces = parserInterface.GetNamespaceChain(pos,this);
			return namespaces;
		}
		public override string[] GetAllVisibleNamespaces(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			List<string> ns = new List<string>();
			ns.AddRange(parserInterface.GetNamespacesVisibleInCurrentScope(pos,this));
			ns.AddRange(parserInterface.GetNamespaceChain(pos,this));
			return ns.ToArray();
		}
		
		public override string GetCurrentTypeFullName(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string typeName = parserInterface.GetCurrentTypeFullName(pos,this);
			return typeName;
		}
		public override string GetCurrentTypeNestedTypePath(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string typeName = parserInterface.GetCurrentTypeNestedTypePath(pos,this);
			return typeName;
		}
		public override string GetCurrentTypeNamespace(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string typeName = parserInterface.GetCurrentTypeNamespace(pos,this);
			return typeName;
		}
		public override string GetCurrentTypeBaseTypeFullName(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string typeName = parserInterface.GetCurrentTypeBaseTypeFullName(pos,this);
			return typeName;
		}
		public override string[] GetCurrentTypeInterfaceNames(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string[] typeName = parserInterface.GetCurrentTypeInterfaceNames(pos,this);
			return typeName;
		}
		
		public override CompletionItem[] GetCurrentVisibleItems(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			CompletionItem[] items = parserInterface.GetCurrentVisibleItems(pos,this);
			return items;
		}
		
		public override bool CheckIfShouldLoad(UIDETextEditor textEditor) {
			return HasFileType(textEditor.extension);
		}
		
		private List<UIDEElement> CreateStringAndCommentElements(UIDELine line, string str) {
			List<UIDEElement> elements = new List<UIDEElement>();
			UIDEElement currentElement = null;
			int c = 0;
			char previousPreviousChar = '\n';
			char previousChar = '\n';
			bool isComment = false;
			bool isBlockComment = false;
			bool isString = false;
			bool isCharString = false;
			while (c < str.Length) {
				char currentChar = str[c];
				//String
				if (!isComment && !isBlockComment && !isCharString) {
					
					if (isString) {
						if (currentChar == '"' && !(previousChar == '\\' && previousPreviousChar != '\\')) {
							isString = false;
							currentElement.rawText += currentChar.ToString();
							currentElement = null;
							previousPreviousChar = previousChar;
							previousChar = currentChar;
							c++;
							continue;
						}
					}
					else {
						if (currentChar == '"' && previousChar != '\\') {
							isString = true;
							currentElement = line.CreateElement("", "String");
							elements.Add(currentElement);
						}
					}
					
				}
				//Char string
				if (!isComment && !isBlockComment && !isString) {
					
					if (isCharString) {
						if (currentChar == '\'' && !(previousChar == '\\' && previousPreviousChar != '\\')) {
							isCharString = false;
							currentElement.rawText += currentChar.ToString();
							currentElement = null;
							previousPreviousChar = previousChar;
							previousChar = currentChar;
							c++;
							continue;
						}
					}
					else {
						if (currentChar == '\'' && previousChar != '\\') {
							isCharString = true;
							currentElement = line.CreateElement("", "String,CharString");
							elements.Add(currentElement);
						}
					}
					
				}
				if (c < str.Length-1) {
					char nextChar = str[c+1];
					//Block comments
					if (!isComment && !isString && !isCharString) {
						char cChar = '/';
						char nChar = '*';
						if (isBlockComment) {
							cChar = '*';
							nChar = '/';
						}
						
						if (currentChar == cChar && nextChar == nChar) {
							if (isBlockComment) {
								isBlockComment = false;
								currentElement.rawText += currentChar.ToString();
								currentElement.rawText += nextChar.ToString();
								currentElement.tokenDef = UIDETokenDefs.Get("Comment,Block,Contained");
								currentElement = null;
								previousPreviousChar = currentChar;
								previousChar = nextChar;
								c++;
								c++;
								continue;
							}
							else {
								isBlockComment = true;
								currentElement = line.CreateElement("", "Comment,Block,Start");
								elements.Add(currentElement);
								currentElement.rawText += currentChar.ToString();
								currentElement.rawText += nextChar.ToString();
								previousPreviousChar = currentChar;
								previousChar = nextChar;
								c++;
								c++;
								continue;
							}
						}
						else {
							if (!isBlockComment) {
								if (currentChar == '*' && nextChar == '/') {
									//a rogue */ so comment out everything up to it.
									elements = new List<UIDEElement>();
									currentElement = line.CreateElement("", "Comment,Block,End");
									currentElement.rawText = line.rawText.Substring(0,c+2);
									elements.Add(currentElement);
									currentElement = null;
									previousPreviousChar = currentChar;
									previousChar = nextChar;
									c++;
									c++;
									continue;
								}
							}
							//else {
							//	if (currentChar == '*' && nextChar == '/') {
							//		isBlockComment = false;
							//		currentElement.tokenDef = UIDETokenDefs.Get("Comment,Block,Contained");
							//		currentElement = null;
							//		previousPreviousChar = currentChar;
							//		previousChar = nextChar;
							//		c++;
							//		c++;
							//	}
							//}
						}
						
					}
					
					//Single line comments
					if (!isString && !isBlockComment && !isCharString) {
						if (currentChar == '/' && nextChar == '/') {
							isComment = true;
							currentElement = line.CreateElement("", "Comment,SingleLine");
							elements.Add(currentElement);
						}
					}
				}
				if (currentElement == null) {
					currentElement = line.CreateElement("", "");
					elements.Add(currentElement);
				}
				
				currentElement.rawText += currentChar.ToString();
				
				previousPreviousChar = previousChar;
				previousChar = currentChar;
				c++;
			}
			return elements;
		}
		
		public override void OnRebuildLineElements(UIDELine line) {
			//line.elements should contain a single element that contains all of its text and has canSplit = true
			List<UIDEElement> elements = line.elements;
			elements = CreateStringAndCommentElements(line,line.rawText);
			
			
			elements = line.CreateSubElements(elements,@"#(.|$)+","PreProcess");
			
			elements = line.CreateSubElements(elements,"\t+","WhiteSpace,Tab");
			elements = line.CreateSubElements(elements,@"\s+","WhiteSpace");
			
			elements = line.CreateSubElements(elements,@"(?<![0-9])[A-Za-z_]+(\w)*","Word");
			//elements = line.CreateSubElements(elements,@"(?<![0-9])[A-Za-z_]+(\w)*","Word,Keyword");
			//elements = line.CreateSubElements(elements,@"(?<![0-9])[A-Za-z_]+(\w)*","Word,Modifier");
			//elements = line.CreateSubElements(elements,@"(?<![0-9])[A-Za-z_]+(\w)*","Word,PrimitiveType");
			
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.?([0-9]+))((E|e)(\+|\-)([0-9]+))?(f|F)","Number,Float");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.([0-9]+))((E|e)(\+|\-)([0-9]+))?(d|D)?","Number,Double");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*)(d|D)","Number,Double");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*)(l|L)","Number,Int64");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*)","Number,Int32");
			//elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.?([0-9]+))((E|e)(\+|\-)([0-9]+))?(f|d|F|D)?","Number");
			
			elements = line.CreateSubElements(elements,@";","LineEnd");
			elements = line.CreateSubElements(elements,@"\.","Dot");
			
			
			line.elements = elements;
			
			wantsMultiLineFormattingUpdate = true;
		}
		
		public bool UpdateMultilineFormatting() {
			if (useMultiThreadingParser) {
				if (updateMultiLineFormattingThread != null && updateMultiLineFormattingThread.IsAlive) {
					wantsMultiLineFormattingUpdate = true;
					return false;
				}
				updateMultiLineFormattingThread = new Thread(UpdateMultilineFormattingActual);
				updateMultiLineFormattingThread.IsBackground = true;
				updateMultiLineFormattingThread.Priority = System.Threading.ThreadPriority.BelowNormal;
				updateMultiLineFormattingThread.Start();
				wantsMultiLineFormattingUpdate = false;
			}
			else {
				UpdateMultilineFormattingActual();
				wantsMultiLineFormattingUpdate = false;
			}
			return true;
		}
		private void UpdateMultilineFormattingActual() {
			
			bool isInBlockComment = false;
			UIDETokenDef multiBlockTokenDef = UIDETokenDefs.Get("Comment,Block,Start");
			//Debug.Log(multiBlockTokenDef.isBold);
			for (int i = 0; i < editor.doc.lineCount; i++) {
				if (i >= editor.doc.lineCount) break;
				UIDELine line = editor.doc.RealLineAt(i);
				if (line == null) continue;
				lock (line) {
					line.overrideTokenDef = null;
					if (!isInBlockComment) {
						if (line.elements.Count > 0) {
							UIDEElement lastElement = line.GetLastNonWhitespaceElement(true);
							if (lastElement != null && lastElement.tokenDef.rawTypes == "Comment,Block,Start") {
								//Debug.Log(lastElement.line.rawText);
								isInBlockComment = true;
							}
						}
					}
					else {
						if (line.elements.Count > 0) {
							UIDEElement firstElement = line.GetFirstNonWhitespaceElement(true);
							if (firstElement != null && firstElement.tokenDef.rawTypes == "Comment,Block,End") {
								isInBlockComment = false;
							}
						}
						if (isInBlockComment) {
							line.overrideTokenDef = multiBlockTokenDef;
						}
					}
				}
			}
			
			if (parserInterface.lastSourceFile == null) return;
			
			lock (parserInterface) {
				bool[] newLineIsFoldable = new bool[editor.doc.lineCount];
				int[] newLineFoldingLength = new int[editor.doc.lineCount];
				ReparseActual();
				List<StatementBlock> blocks = GetStatementBlocksRecursive(parserInterface.lastSourceFile.statementBlock);
				
				foreach (StatementBlock block in blocks) {
					int startLine = block.startLine;
					int endLine = block.endLine;
					if (startLine >= endLine) continue;
					int foldingLength = endLine-startLine;
					UIDELine line = editor.doc.RealLineAt(startLine);
					if (line == null) continue;
					newLineIsFoldable[line.index] = true;
					newLineFoldingLength[line.index] = foldingLength;
					//line.isFoldable = true;
					//line.foldingLength = foldingLength;
					//Debug.Log(blocks.Count+" "+editor.renderedLineCount+" "+line.index);
				}
				
				for (int i = 0; i < newLineIsFoldable.Length; i++) {
					UIDELine line = editor.doc.RealLineAt(i);
					if (line == null) continue;
					line.isFoldable = newLineIsFoldable[i];
					line.foldingLength = newLineFoldingLength[i];
				}
			}
			
			//editor.editorWindow.Repaint();
		}
		
		private List<StatementBlock> GetStatementBlocksRecursive(StatementBlock block) {
			List<StatementBlock> blocks = new List<StatementBlock>();
			foreach (Statement statement in block.statements) {
				if (statement.IsTypeOf<NamespaceDef>()) {
					NamespaceDef ns = (NamespaceDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(ns.statementBlock));
					blocks.Add(ns.statementBlock);
				}
				if (statement.IsTypeOf<TypeDef>()) {
					TypeDef t = (TypeDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(t.statementBlock));
					blocks.Add(t.statementBlock);
				}
				if (statement.IsTypeOf<StatementBlock>()) {
					StatementBlock sb = (StatementBlock)statement;
					blocks.AddRange(GetStatementBlocksRecursive(sb));
					blocks.Add(sb);
				}
				if (statement.IsTypeOf<MethodDef>()) {
					MethodDef m = (MethodDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(m.statementBlock));
					blocks.Add(m.statementBlock);
				}
				if (statement.IsTypeOf<PropertyDef>()) {
					PropertyDef p = (PropertyDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(p.statementBlock));
					blocks.Add(p.statementBlock);
				}
				if (statement.IsTypeOf<ForDef>()) {
					ForDef f = (ForDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(f.statementBlock));
					blocks.Add(f.statementBlock);
				}
				if (statement.IsTypeOf<ForEachDef>()) {
					ForEachDef f = (ForEachDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(f.statementBlock));
					blocks.Add(f.statementBlock);
				}
			}
			return blocks;
		}
		
	}
	
}