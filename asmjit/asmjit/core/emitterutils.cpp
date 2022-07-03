// This file is part of AsmJit project <https://asmjit.com>
//
// See asmjit.h or LICENSE.md for license and copyright information
// SPDX-License-Identifier: Zlib

#include "../core/api-build_p.h"
#include "../core/assembler.h"
#include "../core/emitterutils_p.h"
#include "../core/formatter.h"
#include "../core/logger.h"
#include "../core/support.h"

ASMJIT_BEGIN_NAMESPACE

namespace EmitterUtils {

#ifndef ASMJIT_NO_LOGGING

Error formatLine(String& sb, const uint8_t* binData, size_t binSize, size_t dispSize, size_t immSize, const char* comment) noexcept {
  size_t currentSize = sb.size();
  size_t commentSize = comment ? Support::strLen(comment, Globals::kMaxCommentSize) : 0;

  ASMJIT_ASSERT(binSize >= dispSize);
  const size_t kNoBinSize = SIZE_MAX;

  if ((binSize != 0 && binSize != kNoBinSize) || commentSize) {
    size_t align = kMaxInstLineSize;
    char sep = ';';

    for (size_t i = (binSize == kNoBinSize); i < 2; i++) {
      size_t begin = sb.size();
      ASMJIT_PROPAGATE(sb.padEnd(align));

      if (sep) {
        ASMJIT_PROPAGATE(sb.append(sep));
        ASMJIT_PROPAGATE(sb.append(' '));
      }

      // Append binary data or comment.
      if (i == 0) {
        ASMJIT_PROPAGATE(sb.appendHex(binData, binSize - dispSize - immSize));
        ASMJIT_PROPAGATE(sb.appendChars('.', dispSize * 2));
        ASMJIT_PROPAGATE(sb.appendHex(binData + binSize - immSize, immSize));
        if (commentSize == 0) break;
      }
      else {
        ASMJIT_PROPAGATE(sb.append(comment, commentSize));
      }

      currentSize += sb.size() - begin;
      align += kMaxBinarySize;
      sep = '|';
    }
  }

  return sb.append('\n');
}

void logLabelBound(BaseAssembler* self, const Label& label) noexcept {
  Logger* logger = self->logger();

  StringTmp<512> sb;
  size_t binSize = logger->hasFlag(FormatFlags::kMachineCode) ? size_t(0) : SIZE_MAX;

  sb.appendChars(' ', logger->indentation(FormatIndentationGroup::kLabel));
  Formatter::formatLabel(sb, logger->flags(), self, label.id());
  sb.append(':');
  EmitterUtils::formatLine(sb, nullptr, binSize, 0, 0, self->_inlineComment);
  logger->log(sb.data(), sb.size());
}

void logInstructionEmitted(
  BaseAssembler* self,
  InstId instId,
  InstOptions options,
  const Operand_& o0, const Operand_& o1, const Operand_& o2, const Operand_* opExt,
  uint32_t relSize, uint32_t immSize, uint8_t* afterCursor) {

  Logger* logger = self->logger();
  ASMJIT_ASSERT(logger != nullptr);

  StringTmp<256> sb;
  FormatFlags formatFlags = logger->flags();

  uint8_t* beforeCursor = self->bufferPtr();
  intptr_t emittedSize = (intptr_t)(afterCursor - beforeCursor);

  Operand_ opArray[Globals::kMaxOpCount];
  EmitterUtils::opArrayFromEmitArgs(opArray, o0, o1, o2, opExt);

  sb.appendChars(' ', logger->indentation(FormatIndentationGroup::kCode));
  Formatter::formatInstruction(sb, formatFlags, self, self->arch(), BaseInst(instId, options, self->extraReg()), opArray, Globals::kMaxOpCount);

  if (uint32_t(formatFlags & FormatFlags::kMachineCode) != 0)
    EmitterUtils::formatLine(sb, self->bufferPtr(), size_t(emittedSize), relSize, immSize, self->inlineComment());
  else
    EmitterUtils::formatLine(sb, nullptr, SIZE_MAX, 0, 0, self->inlineComment());
  logger->log(sb);
}

Error logInstructionFailed(
  BaseAssembler* self,
  Error err,
  InstId instId,
  InstOptions options,
  const Operand_& o0, const Operand_& o1, const Operand_& o2, const Operand_* opExt) {

  StringTmp<256> sb;
  sb.append(DebugUtils::errorAsString(err));
  sb.append(": ");

  Operand_ opArray[Globals::kMaxOpCount];
  EmitterUtils::opArrayFromEmitArgs(opArray, o0, o1, o2, opExt);

  Formatter::formatInstruction(sb, FormatFlags::kNone, self, self->arch(), BaseInst(instId, options, self->extraReg()), opArray, Globals::kMaxOpCount);

  if (self->inlineComment()) {
    sb.append(" ; ");
    sb.append(self->inlineComment());
  }

  self->resetInstOptions();
  self->resetExtraReg();
  self->resetInlineComment();
  return self->reportError(err, sb.data());
}

#endif

} // {EmitterUtils}

ASMJIT_END_NAMESPACE
